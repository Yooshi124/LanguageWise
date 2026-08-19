"""Central redaction of registered secrets.

The API key is registered once at startup; every console message and every byte
written to a session or plan artifact passes through `scrub`, so a key that ends
up in a prompt, a human reply or a raw model response never leaks into a file
that might be committed or shared.
"""

from __future__ import annotations

MASK = "[REDACTED]"

# Shorter values are too likely to appear in ordinary text to mask safely.
MIN_SECRET_LENGTH = 8

_secrets: set[str] = set()


def register_secret(value: str | None) -> None:
    """Register a value that must never appear in output."""
    cleaned = (value or "").strip()
    if len(cleaned) >= MIN_SECRET_LENGTH:
        _secrets.add(cleaned)


def scrub(text: str) -> str:
    """Replace every registered secret in `text` with the mask."""
    if not text or not _secrets:
        return text
    for secret in _secrets:
        if secret in text:
            text = text.replace(secret, MASK)
    return text


def clear() -> None:
    """Forget all registered secrets (used by tests)."""
    _secrets.clear()
