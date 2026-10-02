# Progress and handoff

Last updated: 2026-10-02. Status: **complete**.

## Done (merged to `main`)

Each feature went through its own branch and PR, and was squash-merged after CI passed.

| PR | Branch | What it adds |
| --- | --- | --- |
| #1 | `chore/solution-scaffold` | Clean architecture solution on .NET 10 (`Domain → Application → Infrastructure → Api`), unit and integration test projects, pgvector Docker Compose service, GitHub Actions CI, `docs/architecture.md` |
| #2 | `feat/catalogue-import` | `Product` entity, CSV import (`POST /api/catalogue/import`), EF Core + pgvector with an HNSW cosine index, OpenAI and offline local-hashing embedders, background embedding worker, `POST /api/catalogue/embed`, `GET /api/catalogue/stats`, `GET /api/products/{sku}`, 151-product sample catalogue |
| #3 | `feat/vector-search` | `QueryNormalizer` ("2 boxes of the large red peppers" becomes "large red peppers"), `POST /api/match`, `POST /api/match/batch` (one embedding call per batch), 400 problem details on invalid input |
| #4 | `feat/embedding-cache` | Two-tier embedding cache (in-memory LRU, then the Postgres `embedding_cache` table, then the provider). It dedupes within a batch and keys on SHA-256 of model and text. `GET /api/embeddings/cache` reports hits and misses. |
| #5 | `feat/match-history-reranking` | `match_history` table, `POST /api/match/confirm`, `GET /api/customers/{id}/history`, and per-customer re-ranking. The boost is capped and saturates, and phrase-confirmed products are pinned into the candidate pool. |
| #6 | `feat/confidence-thresholds` | `AutoAccept`, `NeedsReview` or `NoMatch` on every result, based on top score and margin, with a readable reason. Thresholds are set per model under `Confidence:Models`. The batch response now includes a per-band `summary`. |

Tests on `main`: 56 unit and 13 integration tests, all passing. The integration tests run against a real `pgvector/pgvector:pg17` container through Testcontainers.

README roadmap status:

- [x] Catalogue import and embedding job
- [x] Vector search endpoint with top-k results
- [x] Embedding cache to cut cost
- [x] Customer match history and re-ranking
- [x] Confidence thresholds for human review
- [x] Benchmark on a sample catalogue

## Benchmark and README (merged)

| PR | Branch | What it adds |
| --- | --- | --- |
| #7 | `feat/benchmark` | `tools/ProductMatcher.Benchmark`, 131 labelled queries, 16 personalisation cases, `docs/benchmark.md`, calibrated per-model thresholds in `appsettings.json` |
| #8 | `docs/readme-results` | README with results, quick start, API table; this file marked complete |

All six roadmap items are done. Tests: 62 unit and 13 integration.

Headline results (OpenAI `text-embedding-3-small` vs the offline embedder):

| Metric | OpenAI | Offline |
| --- | --- | --- |
| Top-1 accuracy | 97.6% | 87.8% |
| Hit@5 | 100% | 95.1% |
| Auto-accept precision / coverage | 99.1% / 86.2% | 98.8% / 69.1% |
| Personalised top-1, before → after | 44% → 100% | 25% → 94% |

## Possible next steps

- Dockerfile for the API, plus an `api` service in compose
- Persisted review queue for `NeedsReview` lines
- Hybrid lexical + vector search. The offline embedder misses abbreviations and synonyms such as "oj", "evoo", "coke" and "serviettes".
- Re-calibrate thresholds on held-out real orders. The current fit is in-sample.
- snake_case column naming (columns are PascalCase, so the raw SQL quotes them)
- Rate limiting on the match endpoints

## Local environment notes

- Compose Postgres runs on **port 5434**, because 5433 is used by `erp-mcp-postgres`. The connection string is in `appsettings.json`.
- Port 5080 is used by another app (AiOps.Api), so run the API locally on 5180: `dotnet run --project src/ProductMatcher.Api --urls http://localhost:5180`.
- EF migrations: `dotnet ef migrations add <Name> -p src/ProductMatcher.Infrastructure -s src/ProductMatcher.Infrastructure -o Persistence/Migrations`. `dotnet-ef` is a local tool (`dotnet-tools.json`), and design-time uses `DesignTimeDbContextFactory`.
- Analyzers run with warnings treated as errors (`latest-recommended`). The test projects relax CA1707, CA2007, CA1515 and CA1861, and migrations are excluded through `.editorconfig`.
- In .NET 10, `dotnet remove <dir> package` fails with "Could not find any project". Pass the `.csproj` path or edit the file directly.
- Local-hashing similarity scores seen before calibration:

  | Query | Top match | Score |
  | --- | --- | --- |
  | large red peppers | FP-1001 | 0.85 |
  | chicken brest | MP-3001 | 0.51 |
  | laptop charger | — | 0.13 |
  | oat milk | DA-2001 (wrong; should be DA-2004) | 0.46 |

  The local embedder can't handle synonyms, which is why the benchmark compares it against OpenAI. Re-run the benchmark after changing the catalogue, the normaliser or the re-ranking weights.
