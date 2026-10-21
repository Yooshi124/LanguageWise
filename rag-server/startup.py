"""Build and verify the bundled corpus before accepting container traffic."""

from __future__ import annotations

import os
import sys

from rag.config import Settings, load_settings
from rag.store import query, rebuild


def prepare_index(settings: Settings) -> int:
	if not settings.corpus_dir.is_dir():
		raise RuntimeError(f"RAG corpus directory does not exist: {settings.corpus_dir}")
	settings.chroma_dir.mkdir(parents=True, exist_ok=True)
	settings.technical_key_path.parent.mkdir(parents=True, exist_ok=True)
	count = rebuild(settings)
	if count == 0:
		raise RuntimeError("RAG corpus contains no indexable documentation.")
	if not query(settings, "How does LanguageWise work?", n_results=1):
		raise RuntimeError("RAG index contains no searchable general documentation.")
	print(f"Indexed and verified {count} documentation chunks.", flush=True)
	return count


def main() -> None:
	prepare_index(load_settings())
	os.execv(sys.executable, [sys.executable, "server.py"])  # noqa: S606 - fixed executable and bundled script


if __name__ == "__main__":
	main()
