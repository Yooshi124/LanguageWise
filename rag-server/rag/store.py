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

# Headings starting with this marker (e.g. `## TECHNICAL-data-model: Tables`) hold internal
# detail for the agentic loop only; their sub-sections inherit the marker.
TECHNICAL_MARKER = re.compile(r"^TECHNICAL-[a-z0-9-]+")


@dataclass(frozen=True)
class RetrievedChunk:
    source: str
    heading: str
    text: str
    distance: float
    technical: bool


@dataclass(frozen=True)
class Chunk:
    heading: str
    text: str
    technical: bool


def get_collection(settings: Settings) -> chromadb.Collection:
    settings.chroma_dir.mkdir(parents=True, exist_ok=True)
    client = chromadb.PersistentClient(path=str(settings.chroma_dir))
    return client.get_or_create_collection(
        name=settings.collection,
        metadata={"hnsw:space": "cosine"},
    )


def chunk_markdown(text: str) -> list[Chunk]:
    """Split markdown into chunks on `#`/`##`/`###` sections.

    Long sections are further divided on blank lines so no chunk exceeds the
    character budget. The nearest preceding heading is attached to every chunk,
    and a chunk is technical when it sits under a TECHNICAL- heading at any level.
    """
    lines = text.splitlines()
    sections: list[tuple[str, bool, list[str]]] = []
    heading = "Overview"
    technical = False
    technical_level: int | None = None
    body: list[str] = []

    for line in lines:
        match = re.match(r"^(#{1,3})\s+(.*)$", line)
        if match:
            if body:
                sections.append((heading, technical, body))
                body = []
            level = len(match.group(1))
            heading = match.group(2).strip()
            if technical_level is None or level <= technical_level:
                technical_level = level if TECHNICAL_MARKER.match(heading) else None
            technical = technical_level is not None
        else:
            body.append(line)
    if body:
        sections.append((heading, technical, body))

    chunks: list[Chunk] = []
    for section_heading, section_technical, section_lines in sections:
        buffer: list[str] = []
        size = 0
        for line in section_lines:
            if size + len(line) > _MAX_CHUNK_CHARS and buffer:
                chunk = "\n".join(buffer).strip()
                if chunk:
                    chunks.append(Chunk(section_heading, chunk, section_technical))
                buffer = []
                size = 0
            buffer.append(line)
            size += len(line) + 1
        chunk = "\n".join(buffer).strip()
        if chunk:
            chunks.append(Chunk(section_heading, chunk, section_technical))
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
    metadatas: list[dict[str, str | bool]] = []

    for path in sorted(settings.corpus_dir.glob("*.md")):
        source = path.stem
        for index, chunk in enumerate(chunk_markdown(path.read_text(encoding="utf-8"))):
            ids.append(f"{source}::{index}")
            documents.append(f"# {source}\n## {chunk.heading}\n\n{chunk.text}")
            metadatas.append({"source": source, "heading": chunk.heading, "technical": chunk.technical})

    if documents:
        collection.add(ids=ids, documents=documents, metadatas=metadatas)
    return len(documents)


def query(
    settings: Settings,
    text: str,
    n_results: int = 5,
    include_technical: bool = False,
) -> list[RetrievedChunk]:
    collection = get_collection(settings)
    if collection.count() == 0:
        return []
    result = collection.query(
        query_texts=[text],
        n_results=min(n_results, collection.count()),
        where=None if include_technical else {"technical": False},
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
                technical=metadata.get("technical") is True,
            )
        )
    return chunks
