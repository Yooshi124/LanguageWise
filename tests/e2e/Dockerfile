FROM python:3.12-slim
WORKDIR /tests
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY *.py ./
CMD ["python", "-m", "pytest", "-q", "--tb=short", "--junitxml=/results/e2e.xml"]
