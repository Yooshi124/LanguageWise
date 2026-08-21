# Release 0 architecture

![Release 0 team microservices architecture](architecture/release-0-architecture.png)

## Overview

LanguageWise is six independent microservices — one per student, plus a team-owned
`shared` service — running together under a single Docker Compose configuration.

Every microservice is built the same way:

| Tier | Technology | Responsibility |
| --- | --- | --- |
| **Frontend** | nginx serving static HTML, CSS and JavaScript (HTMX) | Renders the page. Contains **no** server-side code. |
| **Backend / API** | ASP.NET Core minimal API on .NET 10 | Business logic. Calls the database service over HTTP. |
| **Database service** | ASP.NET Core minimal API on .NET 10 + `Microsoft.Data.Sqlite` | The **only** process that opens the SQLite file. Exposes CRUD. |
| **Database** | SQLite file on a named Docker volume | Persists between restarts. |

## Port map

| Microservice | Owner | Feature | Frontend | Backend | Database service |
| --- | --- | --- | --- | --- | --- |
| `shared` | Team | Unified home page | **3000** | 5000 | 6000 |
| `student-1` | Kyan | Mini Games / Activities | **3001** | 5001 | 6001 |
| `student-2` | Lachlan | Discussion / Chat Forum | **3002** | 5002 | 6002 |
| `student-3` | Justin | Quizzes and Courses | **3003** | 5003 | 6003 |
| `student-4` | Amber | Quests / Achievements | **3004** | 5004 | 6004 |
| `student-5` | Roan | Leaderboard / Analytics | **3005** | 5005 | 6005 |
| `ollama` | Team | LLM runtime | — | 11434 | — |

Inside the Docker network **every .NET container listens on 8080** and every nginx
container listens on 80. The numbers above are the *host* ports published by
Docker Compose, and they match the architecture diagram.

## Request flow

```
Browser
   │  http://localhost:3003/
   ▼
student-3-frontend  (nginx :80)
   │  location /api/  ->  proxy_pass http://student-3-backend:8080
   ▼
student-3-backend   (ASP.NET Core :8080)
   │  HttpClient  ->  http://student-3-db:8080/api/items
   ▼
student-3-db        (ASP.NET Core :8080)
   │  Microsoft.Data.Sqlite
   ▼
/data/student-3.db  (named volume student-3-db-data)
```

### Why nginx proxies `/api/`

The browser only ever talks to one origin, so there is no CORS configuration to get
wrong and no hard-coded backend port in the HTML. The backend ports are still
published on the host, which makes them easy to test directly with `curl` or a REST
client during development.

nginx resolves the backend through Docker's embedded DNS server (`127.0.0.11`) using a
variable in `proxy_pass`. This is deliberate: with a literal upstream name nginx
resolves at start-up and refuses to boot if the backend container is not up yet.

## API contracts

### Database service — `http://localhost:600N`

Full CRUD over the `SampleItems` table.

| Method | Route | Response |
| --- | --- | --- |
| `GET` | `/health` | `200` with the row count, `503` if SQLite is unreachable |
| `GET` | `/api/items` | `200` — every item |
| `GET` | `/api/items/{id}` | `200` or `404` |
| `POST` | `/api/items` | `201` with the created item, `400` if `name` is missing |
| `PUT` | `/api/items/{id}` | `200` with the updated item, `404`, or `400` |
| `DELETE` | `/api/items/{id}` | `204` or `404` |

### Backend / API — `http://localhost:500N`

| Method | Route | Response |
| --- | --- | --- |
| `GET` | `/health` | `200` |
| `GET` | `/api/sample-items` | `200` JSON array, `503` if the database service is down |
| `GET` | `/api/sample-items/fragment` | `200` `text/html` — `<tr>` rows for HTMX |

The fragment endpoint exists because **HTMX swaps HTML, not JSON**. The JSON endpoint
is kept for API consumers and for the Release 1 AI services.

### Frontend — `http://localhost:300N`

| Route | Response |
| --- | --- |
| `/` | `index.html` |
| `/health` | `200` JSON — used by the container health check |
| `/api/*` | Reverse-proxied to the backend |

## The `SampleItems` table

Identical in every database so that the skeleton is easy to follow. Each service seeds
ten rows relevant to its own feature, satisfying the ten-record minimum in
specification section 2.2.

```sql
CREATE TABLE IF NOT EXISTS SampleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    CreatedAt   TEXT NOT NULL
);
```

Schema and seed live in `<service>/database/LanguageWise.<Name>.Db/sql/`. On start-up
the service applies `schema.sql` unconditionally (it is `IF NOT EXISTS`) and runs
`seed.sql` only when the table is empty, so a brand new volume always self-seeds and an
existing volume is never duplicated.

## Docker Compose

19 containers, one bridge network (`languagewise`) and seven named volumes.

Ordering is enforced with `depends_on: { condition: service_healthy }`, so a backend
never starts before its database service reports healthy, and a frontend never starts
before its backend does.

**Every build context is the repository root.** The .NET images need
`Directory.Build.props` and `Directory.Packages.props`, and the student frontend images
copy the shared CSS theme and the vendored HTMX bundle from `shared/frontend/`. That is
how a single theme stays consistent across all six frontends without being duplicated
six times.

## Cross-service calls

The dashed arrows on the diagram (for example `student-1-backend` reading from
`student-2-db`) are not implemented yet, but the topology already supports them: all
containers sit on the same network and can reach each other by container name, e.g.
`http://student-2-db:8080/api/items`.

## Shared CSS theme

`shared/frontend/css/theme.css` is the single source of truth for the look and feel of
the whole application (specification section 2.3). It is copied into every frontend
image at build time. Do not fork it — add new components to it instead.
