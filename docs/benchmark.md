# Benchmark

Generated 2026-10-02 by `tools/ProductMatcher.Benchmark`. All data is synthetic.

```
docker compose up -d
dotnet run --project tools/ProductMatcher.Benchmark -- --providers Local,OpenAI
```

- **Catalogue:** `data/sample-catalogue.csv`, 151 products, with deliberate near-duplicates such as large and small red peppers.
- **Queries:** `data/benchmark-queries.csv`, 131 labelled messy queries: order-phrase 18, shorthand 23, synonym 39, typo 15, variant 28, plus 8 out-of-catalogue queries that should not match.
- **Personalisation:** `data/benchmark-history.csv`, 16 customer phrases, each tested as written and reworded as an order.
- Each query runs through the real `MatchService` (normalise, embed, pgvector HNSW search, classify) with top-k 5 and no customer id.

## Headline

| Metric | Local (`local-hashing-v1`) | OpenAI (`text-embedding-3-small`) |
| --- | --- | --- |
| Top-1 accuracy | 87.8% | 97.6% |
| Hit@3 | 93.5% | 100.0% |
| Hit@5 | 95.1% | 100.0% |
| MRR | 0.910 | 0.988 |
| Auto-accept precision (calibrated) | 98.8% | 99.1% |
| Auto-accept coverage (calibrated) | 69.1% | 86.2% |
| Out-of-catalogue → NoMatch (calibrated) | 100.0% | 37.5% |
| Personalised top-1 before → after | 25.0% → 93.8% | 43.8% → 100.0% |
| Latency p50 / p95, cold | 3.0 ms / 4.3 ms | 203 ms / 272 ms |
| Latency p50 / p95, warm cache | 1.4 ms / 1.6 ms | 2.1 ms / 2.7 ms |
| Catalogue embed time | 406 ms | 1953 ms |

## Top-1 accuracy by query kind

| Kind | Queries | Local (`local-hashing-v1`) | OpenAI (`text-embedding-3-small`) |
| --- | --- | --- | --- |
| order-phrase | 18 | 100.0% | 100.0% |
| shorthand | 23 | 78.3% | 95.7% |
| synonym | 39 | 74.4% | 100.0% |
| typo | 15 | 100.0% | 93.3% |
| variant | 28 | 100.0% | 96.4% |

## Confidence bands

Precision target for auto-accept: **98.0%**, with no out-of-catalogue query auto-accepted. Coverage is the share of in-catalogue queries that are auto-accepted *and* correct, so they need no review.

|  | Local (`local-hashing-v1`) | OpenAI (`text-embedding-3-small`) |
| --- | --- | --- |
| Thresholds in appsettings (auto / margin / review) | 0.40 / 0.10 / 0.225 | 0.40 / 0.02 / 0.275 |
| Recommended thresholds (auto / margin / review) | 0.40 / 0.10 / 0.225 | 0.40 / 0.02 / 0.275 |
| Auto-accepted | 86 | 107 |
| Auto-accept precision | 98.8% | 99.1% |
| Wrong auto-accepts | 1 | 1 |
| Needs review | 31 | 19 |
| In-catalogue sent to NoMatch | 6 | 2 |
| Out-of-catalogue → NoMatch | 8 / 8 | 3 / 8 |

The band rows above use the thresholds in appsettings. Out-of-catalogue queries that miss NoMatch land in NeedsReview, never AutoAccept, so a person still sees them. OpenAI scores unrelated text higher (about 0.25 to 0.35) than the local embedder does, so a higher review floor would start dropping correct matches.

### Threshold sweep: Local

| Auto-accept score | Min margin | Precision | Coverage | Out-of-catalogue accepted |
| --- | --- | --- | --- | --- |
| 0.20 | 0.00 | 89.9% | 87.0% | 0 |
| 0.20 | 0.05 | 95.5% | 85.4% | 0 |
| 0.25 | 0.00 | 91.3% | 85.4% | 0 |
| 0.25 | 0.05 | 96.3% | 83.7% | 0 |
| 0.30 | 0.00 | 92.5% | 80.5% | 0 |
| 0.30 | 0.05 | 97.0% | 79.7% | 0 |
| 0.35 | 0.00 | 94.1% | 78.0% | 0 |
| 0.35 | 0.05 | 96.9% | 77.2% | 0 |
| 0.40 | 0.00 | 96.7% | 72.4% | 0 |
| 0.40 | 0.05 | 97.8% | 72.4% | 0 |
| 0.45 | 0.00 | 100.0% | 67.5% | 0 |
| 0.45 | 0.05 | 100.0% | 67.5% | 0 |
| 0.50 | 0.00 | 100.0% | 59.3% | 0 |
| 0.50 | 0.05 | 100.0% | 59.3% | 0 |
| 0.55 | 0.00 | 100.0% | 52.0% | 0 |
| 0.55 | 0.05 | 100.0% | 52.0% | 0 |
| 0.60 | 0.00 | 100.0% | 43.9% | 0 |
| 0.60 | 0.05 | 100.0% | 43.9% | 0 |
| 0.65 | 0.00 | 100.0% | 36.6% | 0 |
| 0.65 | 0.05 | 100.0% | 36.6% | 0 |
| 0.70 | 0.00 | 100.0% | 30.9% | 0 |
| 0.70 | 0.05 | 100.0% | 30.9% | 0 |
| 0.75 | 0.00 | 100.0% | 22.8% | 0 |
| 0.75 | 0.05 | 100.0% | 22.8% | 0 |
| 0.80 | 0.00 | 100.0% | 16.3% | 0 |
| 0.80 | 0.05 | 100.0% | 16.3% | 0 |

### Threshold sweep: OpenAI

| Auto-accept score | Min margin | Precision | Coverage | Out-of-catalogue accepted |
| --- | --- | --- | --- | --- |
| 0.20 | 0.00 | 91.6% | 97.6% | 8 |
| 0.20 | 0.05 | 99.0% | 80.5% | 1 |
| 0.25 | 0.00 | 93.7% | 95.9% | 5 |
| 0.25 | 0.05 | 99.0% | 80.5% | 1 |
| 0.30 | 0.00 | 94.3% | 94.3% | 4 |
| 0.30 | 0.05 | 99.0% | 78.9% | 1 |
| 0.35 | 0.00 | 95.8% | 92.7% | 2 |
| 0.35 | 0.05 | 100.0% | 78.0% | 0 |
| 0.40 | 0.00 | 97.4% | 90.2% | 0 |
| 0.40 | 0.05 | 100.0% | 76.4% | 0 |
| 0.45 | 0.00 | 97.3% | 88.6% | 0 |
| 0.45 | 0.05 | 100.0% | 74.8% | 0 |
| 0.50 | 0.00 | 97.0% | 79.7% | 0 |
| 0.50 | 0.05 | 100.0% | 67.5% | 0 |
| 0.55 | 0.00 | 97.7% | 69.9% | 0 |
| 0.55 | 0.05 | 100.0% | 60.2% | 0 |
| 0.60 | 0.00 | 100.0% | 51.2% | 0 |
| 0.60 | 0.05 | 100.0% | 45.5% | 0 |
| 0.65 | 0.00 | 100.0% | 34.1% | 0 |
| 0.65 | 0.05 | 100.0% | 30.1% | 0 |
| 0.70 | 0.00 | 100.0% | 16.3% | 0 |

## Embedding cache

|  | Local (`local-hashing-v1`) | OpenAI (`text-embedding-3-small`) |
| --- | --- | --- |
| Queries per pass | 131 | 131 |
| Provider calls, cold pass | 131 | 131 |
| Cold pass hit rate | 0.0% | 0.0% |
| Provider calls, warm pass | 0 | 0 |
| Warm pass hit rate | 100.0% | 100.0% |

Cold-pass hits happen when different queries normalise to the same text. On the warm pass every query is served from the in-memory tier, so the provider isn't called at all.

## Personalisation

Two customers use the same ambiguous words to mean different products. Each customer confirms each phrase 3 times, then the phrase is matched again, both as written and reworded as an order ("2 boxes of … please").

| Customer | Phrase | Means | Local before → after | OpenAI before → after |
| --- | --- | --- | --- | --- |
| bistro-42 | "the usual reds" | `FP-1009` | ❌ → ✅ | ✅ → ✅ |
| bistro-42 | "peppers" | `FP-1002` | ❌ → ✅ | ✅ → ✅ |
| bistro-42 | "cheese" | `DA-2010` | ✅ → ✅ | ✅ → ✅ |
| bistro-42 | "milk" | `DA-2002` | ❌ → ✅ | ❌ → ✅ |
| bistro-42 | "gloves" | `CL-9503` | ✅ → ✅ | ✅ → ✅ |
| bistro-42 | "oil" | `DG-6020` | ❌ → ✅ | ✅ → ✅ |
| bistro-42 | "chips" | `FR-9002` | ✅ → ✅ | ✅ → ✅ |
| bistro-42 | "cream" | `DA-2005` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "the usual reds" | `FP-1006` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "peppers" | `FP-1005` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "cheese" | `DA-2012` | ❌ → ❌ | ❌ → ✅ |
| cafe-7 | "milk" | `DA-2004` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "gloves" | `CL-9502` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "oil" | `DG-6019` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "chips" | `FR-9001` | ❌ → ✅ | ❌ → ✅ |
| cafe-7 | "cream" | `DA-2006` | ✅ → ✅ | ✅ → ✅ |

The boost is capped (0.25 for the phrase plus 0.05 for the product), so history can't push a weak match past a much stronger one. Any ❌ → ❌ row is a case where the customer's product is too far from their wording for that cap. Those phrases stay in NeedsReview rather than being learned blindly.

## Top-1 misses

### Local: 15 misses

| Query | Kind | Expected | Got | Score | Rank of expected | Band |
| --- | --- | --- | --- | --- | --- | --- |
| dbl cream | shorthand | `DA-2005` | `DA-2006` | 0.43 | 2 | NeedsReview |
| evoo | shorthand | `DG-6019` | `CL-9505` | 0.16 | 4 | NoMatch |
| oj | shorthand | `BV-8004` | `FI-4002` | 0.07 | >5 | NoMatch |
| sr flour | shorthand | `DG-6008` | `DG-6007` | 0.43 | 2 | NeedsReview |
| veg stock | shorthand | `DG-6017` | `DG-6018` | 0.39 | 2 | NeedsReview |
| plastic wrap | synonym | `CL-9504` | `BK-5007` | 0.15 | >5 | NoMatch |
| serviettes | synonym | `CL-9510` | `FP-1027` | 0.20 | >5 | NoMatch |
| dish soap | synonym | `CL-9506` | `FP-1017` | 0.24 | >5 | NeedsReview |
| fizzy water | synonym | `BV-8006` | `BV-8005` | 0.38 | 2 | NeedsReview |
| coke | synonym | `BV-8007` | `SC-7001` | 0.26 | >5 | NeedsReview |
| bread rolls | synonym | `BK-5005` | `BK-5002` | 0.35 | 5 | NeedsReview |
| canola oil | synonym | `DG-6020` | `DG-6021` | 0.25 | 2 | NeedsReview |
| pasta twists | synonym | `DG-6006` | `DG-6004` | 0.44 | 2 | AutoAccept |
| garbanzo beans | synonym | `DG-6015` | `BV-8001` | 0.32 | >5 | NeedsReview |
| yogurt plain | synonym | `DA-2016` | `DG-6007` | 0.38 | 2 | NeedsReview |

### OpenAI: 3 misses

| Query | Kind | Expected | Got | Score | Rank of expected | Band |
| --- | --- | --- | --- | --- | --- | --- |
| mayonaise | typo | `SC-7002` | `SC-7009` | 0.51 | 2 | AutoAccept |
| lean mince | shorthand | `MP-3006` | `MP-3005` | 0.59 | 2 | NeedsReview |
| green peppers | variant | `FP-1004` | `FP-1005` | 0.59 | 2 | NeedsReview |

## Caveats

- The catalogue and queries are small and synthetic, and the queries were written by the same author as the catalogue. Treat the numbers as a relative comparison, not production accuracy.
- The thresholds are calibrated on the same queries they're reported on. Re-calibrate on held-out real orders before relying on auto-accept.
- OpenAI embeddings are not exactly deterministic, so its numbers can shift by about a point between runs. The local embedder is fully deterministic.
- Latency is measured in-process against local Postgres. OpenAI cold latency includes a network round trip per query.

## Local Ollama models

Measured 2026-10-03 on the same catalogue and queries, with the models served by a local Ollama (`nomic-embed-text`, 768 dimensions; `bge-m3`, 1024 dimensions). Vectors are zero-padded to the stored 1536 dimensions, which does not change cosine similarity. The OpenAI column above was not re-run in this pass (no API key available), so it is kept from the earlier run. The Local column was re-run and reproduced the figures above exactly (apart from latency).

```
ollama pull nomic-embed-text && ollama pull bge-m3
dotnet run --project tools/ProductMatcher.Benchmark -- --providers Local,Ollama:nomic-embed-text,Ollama:bge-m3
```

The calibrated thresholds in `Confidence:Models` for both models come from this run's sweep (precision target 98%, no out-of-catalogue auto-accept). Cosine scores from these models are compressed into a high range, so their thresholds sit well above OpenAI's. Latency was measured on one developer machine and depends on its hardware.

Caveats specific to these results:

- `bge-m3` sent none of the 8 out-of-catalogue queries to NoMatch at its calibrated review floor (they land in NeedsReview, never AutoAccept). `nomic-embed-text` sent 5 of 8. Both are worse than the offline embedder here, which sent all 8.
- `nomic-embed-text` has 2 wrong auto-accepts at its calibrated thresholds; `bge-m3` has 0 but auto-accepts fewer lines.
- The same small-sample, same-author, calibrated-on-the-test-set caveats in the Caveats section above apply.

### Headline

| Metric | Local (`local-hashing-v1`) | `Ollama:nomic-embed-text` | `Ollama:bge-m3` |
| --- | --- | --- | --- |
| Top-1 accuracy | 87.8% | 91.9% | 91.9% |
| Hit@3 | 93.5% | 97.6% | 97.6% |
| Hit@5 | 95.1% | 98.4% | 97.6% |
| MRR | 0.910 | 0.945 | 0.946 |
| Auto-accept precision (calibrated) | 98.8% | 98.0% | 100.0% |
| Auto-accept coverage (calibrated) | 69.1% | 79.7% | 61.8% |
| Out-of-catalogue → NoMatch (calibrated) | 100.0% | 62.5% | 0.0% |
| Personalised top-1 before → after | 25.0% → 93.8% | 43.8% → 100.0% | 43.8% → 100.0% |
| Latency p50 / p95, cold | 3.4 ms / 5.8 ms | 29 ms / 43 ms | 164 ms / 251 ms |
| Latency p50 / p95, warm cache | 1.4 ms / 1.8 ms | 1.1 ms / 1.2 ms | 1.2 ms / 2.0 ms |
| Catalogue embed time | 434 ms | 1651 ms | 3045 ms |

### Top-1 accuracy by query kind

| Kind | Queries | Local (`local-hashing-v1`) | `Ollama:nomic-embed-text` | `Ollama:bge-m3` |
| --- | --- | --- | --- | --- |
| order-phrase | 18 | 100.0% | 100.0% | 100.0% |
| shorthand | 23 | 78.3% | 95.7% | 95.7% |
| synonym | 39 | 74.4% | 87.2% | 76.9% |
| typo | 15 | 100.0% | 86.7% | 100.0% |
| variant | 28 | 100.0% | 92.9% | 100.0% |

### Confidence bands

Precision target for auto-accept: **98.0%**, with no out-of-catalogue query auto-accepted. Coverage is the share of in-catalogue queries that are auto-accepted *and* correct, so they need no review.

|  | Local (`local-hashing-v1`) | `Ollama:nomic-embed-text` | `Ollama:bge-m3` |
| --- | --- | --- | --- |
| Thresholds in appsettings (auto / margin / review) | 0.40 / 0.10 / 0.225 | 0.65 / 0.00 / 0.575 | 0.60 / 0.00 / 0.400 |
| Recommended thresholds (auto / margin / review) | 0.40 / 0.10 / 0.225 | 0.65 / 0.00 / 0.575 | 0.60 / 0.00 / 0.400 |
| Auto-accepted | 86 | 100 | 76 |
| Auto-accept precision | 98.8% | 98.0% | 100.0% |
| Wrong auto-accepts | 1 | 2 | 0 |
| Needs review | 31 | 19 | 54 |
| In-catalogue sent to NoMatch | 6 | 7 | 1 |
| Out-of-catalogue → NoMatch | 8 / 8 | 5 / 8 | 0 / 8 |

The band rows above use the thresholds in appsettings. Out-of-catalogue queries that miss NoMatch land in NeedsReview, never AutoAccept, so a person still sees them.
