# 🔎 Semantic Product Matcher

![Status](https://img.shields.io/badge/status-complete-brightgreen?style=for-the-badge) ![.NET](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white) ![pgvector](https://img.shields.io/badge/pgvector-336791?style=for-the-badge&logo=postgresql&logoColor=white) ![OpenAI](https://img.shields.io/badge/OpenAI-412991?style=for-the-badge&logo=openai&logoColor=white)

> Match messy, customer-written product names to the right catalogue item using embeddings and vector search.

## 🎯 Why this project

Customers rarely use exact product codes: they write "2 boxes of the large red peppers". This service:

- embeds the catalogue and searches it with pgvector
- learns from confirmed matches, so each customer's usual phrasing ranks higher next time
- tells you which lines are safe to auto-accept and which need a person to check

## 📊 Results

Measured on a synthetic 151-product catering catalogue with 131 labelled messy queries. The queries cover order phrasing, typos, shorthand, synonyms, near-variants and out-of-catalogue items. Full report: [docs/benchmark.md](docs/benchmark.md).

| Metric | Offline embedder | Ollama `nomic-embed-text` | Ollama `bge-m3` | OpenAI `text-embedding-3-small` |
| --- | --- | --- | --- | --- |
| Top-1 accuracy | 87.8% | 91.9% | 91.9% | **97.6%** |
| Hit@5 | 95.1% | 98.4% | 97.6% | **100%** |
| Auto-accept precision / coverage | 98.8% / 69.1% | 98.0% / 79.7% | 100% / 61.8% | **99.1% / 86.2%** |
| Personalised top-1, before → after 3 confirmations | 25% → 94% | 44% → 100% | 44% → 100% | 44% → **100%** |
| Latency p50, cold → cached | 3.4 → 1.4 ms | 29 → 1.1 ms | 164 → 1.2 ms | 203 → **2.1 ms** |

The Ollama and offline columns were measured on 2026-10-03 on a developer machine; the OpenAI column is from the earlier 2026-10-02 run. Local-model latency depends on your hardware.

With OpenAI, 86% of order lines are matched automatically at 99% precision. The rest go to review, and an out-of-catalogue item is never auto-accepted. A repeat order makes **zero** embedding API calls because of the cache.

## 🧱 Stack

- .NET 10 minimal API, clean architecture (`Domain → Application → Infrastructure → Api`)
- PostgreSQL 17 with pgvector: an HNSW cosine index, accessed through EF Core and Npgsql
- OpenAI embeddings, or a deterministic offline hashing embedder that needs no API key (used by default and in tests)
- A two-tier embedding cache: in-memory LRU, then a Postgres table
- A match-history table that re-ranks results per customer
- Docker Compose for local setup; xUnit with Testcontainers for tests; GitHub Actions CI

See [docs/architecture.md](docs/architecture.md) for the layers and the request flow.

## 🚀 Quick start

```bash
docker compose up -d                                   # pgvector on localhost:5434
dotnet run --project src/ProductMatcher.Api --urls http://localhost:5180

curl -F file=@data/sample-catalogue.csv localhost:5180/api/catalogue/import
curl -X POST localhost:5180/api/catalogue/embed        # or let the background job do it

curl localhost:5180/api/match -H 'content-type: application/json' \
  -d '{"query":"2 boxes of the large red peppers","topK":3}'
```

To use OpenAI instead of the offline embedder, set `OPENAI_API_KEY` and `Embeddings__Provider=OpenAI`. Products embedded with the previous model are detected and re-embedded automatically.

## 🏠 Run fully locally

No API key and no network: embed with a model served by [Ollama](https://ollama.com). The models are 768 and 1024 dimensions, so vectors are zero-padded to the stored 1536 (this leaves cosine similarity unchanged, and the schema is untouched).

```bash
ollama pull nomic-embed-text          # or: ollama pull bge-m3
docker compose up -d
Embeddings__Provider=Ollama Embeddings__Model=nomic-embed-text \
  dotnet run --project src/ProductMatcher.Api --urls http://localhost:5180
```

`Embeddings:BaseUrl` defaults to `http://localhost:11434` and `Embeddings:TimeoutSeconds` to 120. Confidence thresholds for both models are in `Confidence:Models`. Products embedded with a different model are detected and re-embedded automatically, and the embedding cache is keyed by model, so vectors from different models are never mixed. For a fully offline run with no model server at all, use `Embeddings__Provider=Local`.

## 🔌 API

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/api/catalogue/import` | Upsert products from CSV, sent as multipart `file` or a `text/csv` body. Reports errors per line. |
| `POST` | `/api/catalogue/embed` | Embed pending products now |
| `GET` | `/api/catalogue/stats` | Product count and embedding progress |
| `GET` | `/api/products/{sku}` | One product |
| `POST` | `/api/match` | `{ query, topK?, customerId? }` returns ranked candidates and a decision |
| `POST` | `/api/match/batch` | `{ queries[], topK?, customerId? }` returns results plus a per-band summary |
| `POST` | `/api/match/confirm` | `{ customerId, query, sku }` records what the customer meant |
| `GET` | `/api/customers/{id}/history` | Recent confirmations |
| `GET` | `/api/embeddings/cache` | Cache hits, misses and provider calls |
| `GET` | `/health` | Liveness |

An example decision from `/api/match`:

```json
{
  "query": "2 boxes of the large red peppers",
  "normalizedQuery": "large red peppers",
  "decision": { "band": "AutoAccept", "sku": "FP-1001", "topScore": 0.85, "margin": 0.25, "reason": "Strong, unambiguous match." },
  "candidates": [ { "sku": "FP-1001", "name": "Red Peppers Large", "similarity": 0.85, "boost": 0, "score": 0.85 } ]
}
```

## 🧪 Tests and benchmark

```bash
dotnet test                                                        # 72 unit + 13 integration (needs Docker)
dotnet run --project tools/ProductMatcher.Benchmark -- --providers Local,OpenAI,Ollama:nomic-embed-text,Ollama:bge-m3
```

The benchmark regenerates [docs/benchmark.md](docs/benchmark.md) and recommends confidence thresholds for each model. The current ones are in `Confidence:Models` in `src/ProductMatcher.Api/appsettings.json`.

## 🗺️ Roadmap

- [x] Catalogue import and embedding job
- [x] Vector search endpoint with top-k results
- [x] Embedding cache to cut cost
- [x] Customer match history and re-ranking
- [x] Confidence thresholds for human review
- [x] Benchmark on a sample catalogue

Possible next steps:

- an API Dockerfile
- a persisted review queue for `NeedsReview` lines
- re-calibrating thresholds on held-out real orders
- a hybrid lexical + vector search to catch abbreviations like "oj" and "evoo"

## 📌 Status

✅ Complete. It uses synthetic sample data only.

---

Built by [Jahanzaib Ahmad](https://github.com/jehanxaibahmed) · Full Stack Engineer · AI & LLM Systems
