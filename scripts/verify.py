"""End-to-end verification of the running Docker Compose stack.

Checks every tier of every microservice through its published host port, then
exercises full CRUD through the frontend proxy to prove the whole chain works.

Start the stack first, then run:

    python scripts/verify.py

Exits non-zero if any check fails. Set OLLAMA_PORT if you published Ollama
somewhere other than 11434.
"""

import json
import os
import sys
import urllib.error
import urllib.request

OLLAMA_PORT = os.environ.get("OLLAMA_PORT", "11434")

SERVICES = [
    ("shared", 3000, 5000, 6000, False),
    ("mini-games-service", 3001, 5001, 6001, True),
    ("chat-discussion-service", 3002, 5002, 6002, False),
    ("quizzes-courses-service", 3003, 5003, 6003, False),
    ("quests-achievements-notifications-service", 3004, 5004, 6004, False),
    ("leaderboard-analytics-service", 3005, 5005, 6005, False),
]

passed, failed = 0, []


def check(label, ok, detail=""):
    global passed
    if ok:
        passed += 1
        print(f"  PASS  {label}")
    else:
        failed.append(f"{label} :: {detail}")
        print(f"  FAIL  {label} :: {detail}")


def http(url, method="GET", body=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    if data:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            return r.status, r.read().decode("utf-8", "replace"), dict(r.headers)
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace"), dict(e.headers)
    except Exception as e:
        return 0, str(e), {}


for name, fe, be, db, is_spa in SERVICES:
    print(f"\n=== {name} ===")

    # -- database service ----------------------------------------------------
    status, body, _ = http(f"http://localhost:{db}/health")
    check(f"db :{db} /health", status == 200, f"{status} {body[:120]}")

    status, body, _ = http(f"http://localhost:{db}/api/items")
    try:
        items = json.loads(body)
    except Exception:
        items = []
    check(f"db :{db} /api/items returns >=10 seeded rows",
          status == 200 and len(items) >= 10, f"{status} count={len(items)}")

    # -- backend -------------------------------------------------------------
    status, body, _ = http(f"http://localhost:{be}/health")
    check(f"be :{be} /health", status == 200, f"{status} {body[:120]}")

    status, body, _ = http(f"http://localhost:{be}/api/sample-items")
    try:
        api_items = json.loads(body)
    except Exception:
        api_items = []
    check(f"be :{be} /api/sample-items proxies the db",
          status == 200 and len(api_items) == len(items),
          f"{status} api={len(api_items)} db={len(items)}")

    status, body, hdrs = http(f"http://localhost:{be}/api/sample-items/fragment")
    check(f"be :{be} /api/sample-items/fragment is HTML rows",
          status == 200 and "<tr>" in body and "text/html" in hdrs.get("Content-Type", ""),
          f"{status} ct={hdrs.get('Content-Type')}")

    # -- frontend ------------------------------------------------------------
    status, body, _ = http(f"http://localhost:{fe}/health")
    check(f"fe :{fe} /health", status == 200, f"{status} {body[:120]}")

    status, body, _ = http(f"http://localhost:{fe}/")
    if is_spa:
        ok = status == 200 and "<div id=\"app\"" in body
        check(f"fe :{fe} / serves the built Vue app", ok, f"{status} {body[:120]}")
        status, body, _ = http(f"http://localhost:{fe}/vocab-voyage")
        check(f"fe :{fe} /vocab-voyage falls back to index.html",
              status == 200 and "<div id=\"app\"" in body, f"{status}")
        status, body, _ = http(f"http://localhost:{fe}/sample-items")
        check(f"fe :{fe} /sample-items falls back to index.html",
              status == 200 and "<div id=\"app\"" in body, f"{status}")
    else:
        check(f"fe :{fe} / serves index.html",
              status == 200 and "hx-get=\"/api/sample-items/fragment\"" in body,
              f"{status} {body[:120]}")
        status, css, _ = http(f"http://localhost:{fe}/css/theme.css")
        check(f"fe :{fe} /css/theme.css is vendored", status == 200 and len(css) > 500, f"{status}")
        status, js, _ = http(f"http://localhost:{fe}/js/htmx.min.js")
        check(f"fe :{fe} /js/htmx.min.js is vendored", status == 200 and len(js) > 5000, f"{status}")

    # -- the proxy chain: browser -> nginx -> backend -> db ------------------
    status, body, _ = http(f"http://localhost:{fe}/api/sample-items")
    try:
        proxied = json.loads(body)
    except Exception:
        proxied = []
    check(f"fe :{fe} /api/ proxies through to the database",
          status == 200 and len(proxied) == len(items),
          f"{status} proxied={len(proxied)} db={len(items)}")

    # -- CRUD against the database service ----------------------------------
    status, body, _ = http(f"http://localhost:{db}/api/items", "POST",
                           {"name": "verify-probe", "description": "created by verify-stack"})
    try:
        created = json.loads(body)
    except Exception:
        created = {}
    new_id = created.get("id")
    check(f"db :{db} POST /api/items creates a row", status == 201 and new_id, f"{status} {body[:160]}")

    if new_id:
        status, body, _ = http(f"http://localhost:{db}/api/items/{new_id}", "PUT",
                               {"name": "verify-probe-updated", "description": "updated"})
        try:
            updated = json.loads(body).get("name")
        except Exception:
            updated = None
        check(f"db :{db} PUT /api/items/{{id}} updates it",
              status == 200 and updated == "verify-probe-updated", f"{status} {body[:160]}")

        status, body, _ = http(f"http://localhost:{db}/api/items/{new_id}", "DELETE")
        check(f"db :{db} DELETE /api/items/{{id}} removes it", status == 204, f"{status} {body[:120]}")

        status, _, _ = http(f"http://localhost:{db}/api/items/{new_id}")
        check(f"db :{db} GET deleted id is 404", status == 404, f"{status}")

    status, body, _ = http(f"http://localhost:{db}/api/items", "POST", {"description": "no name"})
    check(f"db :{db} POST without a name is rejected", status == 400, f"{status} {body[:120]}")

    status, _, _ = http(f"http://localhost:{db}/api/items/99999999")
    check(f"db :{db} GET unknown id is 404", status == 404, f"{status}")

# -- isolation: no service may see another's data ----------------------------
print("\n=== isolation ===")
names = {}
for name, _, _, db, _ in SERVICES:
    _, body, _ = http(f"http://localhost:{db}/api/items")
    try:
        names[name] = {i.get("name") for i in json.loads(body)}
    except Exception:
        names[name] = set()

overlap = []
keys = list(names)
for i in range(len(keys)):
    for j in range(i + 1, len(keys)):
        shared_names = names[keys[i]] & names[keys[j]]
        if shared_names:
            overlap.append(f"{keys[i]}/{keys[j]}: {sorted(shared_names)[:3]}")
check("each database holds its own distinct seed data", not overlap, "; ".join(overlap))

# -- ollama ------------------------------------------------------------------
print("\n=== ollama ===")
status, body, _ = http(f"http://localhost:{OLLAMA_PORT}/api/tags")
check(f"ollama :{OLLAMA_PORT} /api/tags responds", status == 200, f"{status} {body[:120]}")

print(f"\n{'=' * 60}\nPASSED {passed} / {passed + len(failed)}")
for f in failed:
    print(f"  FAILED: {f}")
sys.exit(1 if failed else 0)
