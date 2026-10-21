"""Run explicitly while RAG and the games backend are stopped by the launcher."""

import httpx

from test_e2e import FEATURES, SHARED, account, call_tool


def test_unavailable_rag_and_downstream_are_not_successful_empty_results():
	with httpx.Client(timeout=30, trust_env=False) as http:
		user = account(http)
		response = http.post(f"{SHARED}/api/rag/answer", headers=user["headers"],
			json={"query": FEATURES[1].query})
		assert response.status_code == 502, response.text
		docs = call_tool(http, "courses", user["token"], "docs_search", {"query": FEATURES[1].query})
		assert docs["result"]["isError"] is True, docs
		games = call_tool(http, "games", user["token"], "games_get_completion_stats")
		assert games["result"]["isError"] is True, games
