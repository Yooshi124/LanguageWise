"""Runtime configuration for the RAG server, loaded from environment/.env."""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

from dotenv import load_dotenv

SERVER_ROOT = Path(__file__).resolve().parent.parent

load_dotenv(SERVER_ROOT / ".env")


def _resolve(path_value: str) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else SERVER_ROOT / path


@dataclass(frozen=True)
class Settings:
    host: str
    port: int
    chroma_dir: Path
    corpus_dir: Path
    collection: str


def load_settings() -> Settings:
    return Settings(
        host=os.getenv("RAG_HOST", "0.0.0.0"),
        port=int(os.getenv("RAG_PORT", "8100")),
        chroma_dir=_resolve(os.getenv("RAG_CHROMA_DIR", ".chroma")),
        corpus_dir=_resolve(os.getenv("RAG_CORPUS_DIR", "corpus")),
        collection=os.getenv("RAG_COLLECTION", "languagewise-docs"),
    )
