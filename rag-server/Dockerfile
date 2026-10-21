FROM python:3.12-slim
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1 \
	PYTHONUNBUFFERED=1 \
	HOME=/home/app \
	RAG_HOST=0.0.0.0 \
	RAG_PORT=8100 \
	RAG_CHROMA_DIR=/data/chroma \
	RAG_TECHNICAL_KEY_PATH=/data/.rag-technical-key
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
	&& useradd --create-home --uid 10001 app \
	&& mkdir -p /data /home/app/.cache/chroma \
	&& chown -R app:app /data /home/app
COPY rag/ rag/
COPY corpus/ corpus/
COPY server.py ingest.py startup.py ./
USER app
RUN python -c "from chromadb.utils.embedding_functions import DefaultEmbeddingFunction; DefaultEmbeddingFunction()(['LanguageWise model cache warmup'])"
EXPOSE 8100
HEALTHCHECK --interval=10s --timeout=5s --start-period=300s --retries=6 CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8100/health', timeout=3)" || exit 1
ENTRYPOINT ["python", "startup.py"]
