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

| Metric | Offline embedder | OpenAI `text-embedding-3-small` |
| --- | --- | --- |
| Top-1 accuracy | 87.8% | **97.6%** |
| Hit@5 | 95.1% | **100%** |
| Auto-accept precision / coverage | 98.8% / 69.1% | **99.1% / 86.2%** |
| Personalised top-1, before → after 3 confirmations | 25% → 94% | 44% → **100%** |
| Latency p50, cold → cached | 3.0 → 1.4 ms | 203 → **2.1 ms** |

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
dotnet test                                                        # 62 unit + 13 integration (needs Docker)
dotnet run --project tools/ProductMatcher.Benchmark -- --providers Local,OpenAI
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
