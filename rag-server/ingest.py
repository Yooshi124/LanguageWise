"""Rebuild the ChromaDB index from the corpus.

Run from the rag-server directory after activating your virtual environment:

    python ingest.py
"""

from __future__ import annotations

from rag.config import load_settings
from rag.store import rebuild


def main() -> None:
    settings = load_settings()
    print(f"Corpus:     {settings.corpus_dir}")
    print(f"Chroma dir: {settings.chroma_dir}")
    print(f"Collection: {settings.collection}")
    count = rebuild(settings)
    print(f"Indexed {count} chunk(s).")


if __name__ == "__main__":
    main()
