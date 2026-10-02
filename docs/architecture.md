# Architecture

The solution follows a clean architecture layout. Dependencies point inwards only.

```
Api ──► Infrastructure ──► Application ──► Domain
```

| Project | Responsibility |
| --- | --- |
| `ProductMatcher.Domain` | Entities and value objects: products, match history, confidence bands. No dependencies. |
| `ProductMatcher.Application` | Use cases and ports: catalogue import, matching, re-ranking, confidence classification. Defines interfaces such as `IEmbeddingProvider` and `IProductRepository`. |
| `ProductMatcher.Infrastructure` | Adapters: EF Core + Npgsql + pgvector persistence, OpenAI embeddings, the embedding cache. |
| `ProductMatcher.Api` | ASP.NET Core minimal API host, composition root and HTTP endpoints. |
| `tools/ProductMatcher.Benchmark` | Console app that measures match accuracy on the sample catalogue. |
| `tests/*` | Unit tests (pure logic) and integration tests (real Postgres via Testcontainers). |

## Request flow: `POST /api/match`

1. The query text is normalised (lower case, quantities and filler words such as "2 boxes of" removed).
2. The embedding is read from the cache, or fetched from the provider and stored.
3. pgvector returns the top-k nearest products by cosine distance.
4. When a customer id is supplied, products that customer confirmed before are boosted.
5. The top score is mapped to a confidence band: `AutoAccept`, `NeedsReview` or `NoMatch`.
