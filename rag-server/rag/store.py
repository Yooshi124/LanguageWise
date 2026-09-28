"""ChromaDB store: corpus chunking, ingestion, and semantic retrieval.

Uses ChromaDB's built-in default embedding function (all-MiniLM-L6-v2 via ONNX)
so the server needs no external embedding API key. The model is downloaded once
on first use and cached locally.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

import chromadb

from rag.config import Settings

# Roughly targets a few hundred tokens per chunk while keeping headings intact.
_MAX_CHUNK_CHARS = 1200


@dataclass(frozen=True)
class RetrievedChunk:
    source: str
    heading: str
    text: str
    distance: float


def get_collection(settings: Settings) -> chromadb.Collection:
    settings.chroma_dir.mkdir(parents=True, exist_ok=True)
    client = chromadb.PersistentClient(path=str(settings.chroma_dir))
    return client.get_or_create_collection(
        name=settings.collection,
        metadata={"hnsw:space": "cosine"},
    )


def chunk_markdown(text: str) -> list[tuple[str, str]]:
    """Split markdown into (heading, chunk) pairs, splitting on `#`/`##` sections.

    Long sections are further divided on blank lines so no chunk exceeds the
    character budget. The nearest preceding heading is attached to every chunk.
    """
    lines = text.splitlines()
    sections: list[tuple[str, list[str]]] = []
    heading = "Overview"
    body: list[str] = []

    for line in lines:
        if re.match(r"^#{1,3}\s+", line):
            if body:
                sections.append((heading, body))
                body = []
            heading = line.lstrip("#").strip()
        else:
            body.append(line)
    if body:
        sections.append((heading, body))

    chunks: list[tuple[str, str]] = []
    for section_heading, section_lines in sections:
        buffer: list[str] = []
        size = 0
        for line in section_lines:
            if size + len(line) > _MAX_CHUNK_CHARS and buffer:
                chunk = "\n".join(buffer).strip()
                if chunk:
                    chunks.append((section_heading, chunk))
                buffer = []
                size = 0
            buffer.append(line)
            size += len(line) + 1
        chunk = "\n".join(buffer).strip()
        if chunk:
            chunks.append((section_heading, chunk))
    return chunks


def rebuild(settings: Settings) -> int:
    """Drop and repopulate the collection from every markdown file in the corpus."""
    client = chromadb.PersistentClient(path=str(settings.chroma_dir))
    try:
        client.delete_collection(settings.collection)
    except Exception:
        pass  # Collection may not exist yet on first run.
    collection = client.get_or_create_collection(
        name=settings.collection,
        metadata={"hnsw:space": "cosine"},
    )

    ids: list[str] = []
    documents: list[str] = []
    metadatas: list[dict[str, str]] = []

    for path in sorted(settings.corpus_dir.glob("*.md")):
        source = path.stem
        for index, (heading, chunk) in enumerate(chunk_markdown(path.read_text(encoding="utf-8"))):
            ids.append(f"{source}::{index}")
            documents.append(f"# {source}\n## {heading}\n\n{chunk}")
            metadatas.append({"source": source, "heading": heading})

    if documents:
        collection.add(ids=ids, documents=documents, metadatas=metadatas)
    return len(documents)


def query(settings: Settings, text: str, n_results: int = 5) -> list[RetrievedChunk]:
    collection = get_collection(settings)
    if collection.count() == 0:
        return []
    result = collection.query(
        query_texts=[text],
        n_results=min(n_results, collection.count()),
    )
    documents = result.get("documents", [[]])[0]
    metadatas = result.get("metadatas", [[]])[0]
    distances = result.get("distances", [[]])[0]

    chunks: list[RetrievedChunk] = []
    for document, metadata, distance in zip(documents, metadatas, distances):
        chunks.append(
            RetrievedChunk(
                source=str(metadata.get("source", "unknown")),
                heading=str(metadata.get("heading", "")),
                text=document,
                distance=float(distance),
            )
        )
    return chunks
