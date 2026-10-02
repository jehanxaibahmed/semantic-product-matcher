using System.Globalization;
using System.Text;

namespace ProductMatcher.Benchmark;

/// <summary>Renders the comparison as docs/benchmark.md.</summary>
internal static class ReportWriter
{
    private const int MaxMissesListed = 25;

    public static string Render(IReadOnlyList<ProviderResult> results, DateTimeOffset generatedAt)
    {
        var sb = new StringBuilder();
        var names = results.Select(r => r.Provider.Contains(r.Model) ? $"`{r.Provider}`" : $"{r.Provider} (`{r.Model}`)").ToList();

        Line(sb, "# Benchmark");
        Line(sb);
        Line(sb, $"Generated {generatedAt:yyyy-MM-dd} by `tools/ProductMatcher.Benchmark`. All data is synthetic.");
        Line(sb);
        Line(sb, "```");
        Line(sb, "docker compose up -d");
        Line(sb, "dotnet run --project tools/ProductMatcher.Benchmark -- --providers Local,OpenAI,Ollama:nomic-embed-text,Ollama:bge-m3");
        Line(sb, "```");
        Line(sb);
        var first = results[0];
        var kinds = Analysis.Accuracy(first.Cold).ByKind;
        Line(sb, $"- **Catalogue:** `data/sample-catalogue.csv`, {first.CatalogueSize} products, with deliberate near-duplicates such as large and small red peppers.");
        Line(sb, $"- **Queries:** `data/benchmark-queries.csv`, {first.Cold.Count} labelled messy queries: " +
                 string.Join(", ", kinds.Select(k => $"{k.Key} {k.Value.Count}")) +
                 $", plus {first.Cold.Count(o => !o.InCatalogue)} out-of-catalogue queries that should not match.");
        Line(sb, $"- **Personalisation:** `data/benchmark-history.csv`, {first.History.Count / 2} customer phrases, each tested as written and reworded as an order.");
        Line(sb, "- Each query runs through the real `MatchService` (normalise, embed, pgvector HNSW search, classify) with top-k 5 and no customer id.");
        Line(sb);

        Line(sb, "## Headline");
        Line(sb);
        Header(sb, ["Metric", .. names]);
        Row(sb, "Top-1 accuracy", results.Select(r => Pct(Analysis.Accuracy(r.Cold).Top1)));
        Row(sb, "Hit@3", results.Select(r => Pct(Analysis.Accuracy(r.Cold).HitAt3)));
        Row(sb, "Hit@5", results.Select(r => Pct(Analysis.Accuracy(r.Cold).HitAt5)));
        Row(sb, "MRR", results.Select(r => F(Analysis.Accuracy(r.Cold).Mrr, 3)));
        Row(sb, "Auto-accept precision (calibrated)", results.Select(r => Calibrated(r, c => Pct(c.Precision))));
        Row(sb, "Auto-accept coverage (calibrated)", results.Select(r => Calibrated(r, c => Pct(c.Coverage))));
        Row(sb, "Out-of-catalogue → NoMatch (calibrated)", results.Select(r => Pct(Analysis.Calibrate(r.Cold).OutOfCatalogueNoMatchRecall)));
        Row(sb, "Personalised top-1 before → after", results.Select(r =>
            $"{Pct(Share(r.History, h => h.CorrectBefore))} → {Pct(Share(r.History, h => h.CorrectAfter))}"));
        Row(sb, "Latency p50 / p95, cold", results.Select(r =>
            $"{Ms(Analysis.Percentile(r.Cold.Select(o => o.LatencyMs).ToList(), 50))} / {Ms(Analysis.Percentile(r.Cold.Select(o => o.LatencyMs).ToList(), 95))}"));
        Row(sb, "Latency p50 / p95, warm cache", results.Select(r =>
            $"{Ms(Analysis.Percentile(r.WarmLatenciesMs, 50))} / {Ms(Analysis.Percentile(r.WarmLatenciesMs, 95))}"));
        Row(sb, "Catalogue embed time", results.Select(r => Ms(r.CatalogueEmbedMs)));
        Line(sb);

        Line(sb, "## Top-1 accuracy by query kind");
        Line(sb);
        Header(sb, ["Kind", "Queries", .. names]);
        foreach (var kind in kinds.Keys)
        {
            Row(sb, kind, [kinds[kind].Count.ToString(CultureInfo.InvariantCulture),
                .. results.Select(r => Pct(Analysis.Accuracy(r.Cold).ByKind[kind].Top1))]);
        }

        Line(sb);

        Line(sb, "## Confidence bands");
        Line(sb);
        Line(sb, $"Precision target for auto-accept: **{Pct(Analysis.TargetPrecision)}**, with no out-of-catalogue query auto-accepted. " +
                 "Coverage is the share of in-catalogue queries that are auto-accepted *and* correct, so they need no review.");
        Line(sb);
        Header(sb, ["", .. names]);
        Row(sb, "Thresholds in appsettings (auto / margin / review)", results.Select(r =>
            $"{F(r.Thresholds.AutoAcceptScore)} / {F(r.Thresholds.MinMargin)} / {F(r.Thresholds.ReviewScore, 3)}"));
        Row(sb, "Recommended thresholds (auto / margin / review)", results.Select(r =>
        {
            var c = Analysis.Calibrate(r.Cold);
            return c.AutoAccept is { } a ? $"{F(a.AutoAcceptScore)} / {F(a.MinMargin)} / {F(c.ReviewScore, 3)}" : $"none reach target / – / {F(c.ReviewScore, 3)}";
        }));
        Row(sb, "Auto-accepted", results.Select(r => Analysis.Bands(r.Cold).AutoAccepted.ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Auto-accept precision", results.Select(r => Pct(Analysis.Bands(r.Cold).AutoAcceptPrecision)));
        Row(sb, "Wrong auto-accepts", results.Select(r => Analysis.Bands(r.Cold).WrongAutoAccepts.ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Needs review", results.Select(r => Analysis.Bands(r.Cold).NeedsReview.ToString(CultureInfo.InvariantCulture)));
        Row(sb, "In-catalogue sent to NoMatch", results.Select(r => Analysis.Bands(r.Cold).NoMatchInCatalogue.ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Out-of-catalogue → NoMatch", results.Select(r =>
        {
            var b = Analysis.Bands(r.Cold);
            return $"{b.OutOfCatalogueNoMatch} / {b.OutOfCatalogue}";
        }));
        Line(sb);
        Line(sb, "The band rows above use the thresholds in appsettings. Out-of-catalogue queries that miss NoMatch land in NeedsReview, never AutoAccept, so a person still sees them. "
                 + "OpenAI scores unrelated text higher (about 0.25 to 0.35) than the local embedder does, so a higher review floor would start dropping correct matches.");
        Line(sb);

        foreach (var r in results)
        {
            Line(sb, $"### Threshold sweep: {r.Provider}");
            Line(sb);
            Header(sb, ["Auto-accept score", "Min margin", "Precision", "Coverage", "Out-of-catalogue accepted"]);
            foreach (var row in Analysis.Sweep(r.Cold).Where(s => s.MinMargin is 0.05 or 0.0 && s.Coverage >= 0.15))
            {
                Row(sb, F(row.AutoAcceptScore), [F(row.MinMargin), Pct(row.Precision), Pct(row.Coverage),
                    row.OutOfCatalogueAutoAccepted.ToString(CultureInfo.InvariantCulture)]);
            }

            Line(sb);
        }

        Line(sb, "## Embedding cache");
        Line(sb);
        Header(sb, ["", .. names]);
        Row(sb, "Queries per pass", results.Select(r => r.Cold.Count.ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Provider calls, cold pass", results.Select(r =>
            (r.CacheAfterCold.ProviderCalls - r.CacheBeforeCold.ProviderCalls).ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Cold pass hit rate", results.Select(r => HitRate(r.CacheBeforeCold, r.CacheAfterCold)));
        Row(sb, "Provider calls, warm pass", results.Select(r =>
            (r.CacheAfterWarm.ProviderCalls - r.CacheAfterCold.ProviderCalls).ToString(CultureInfo.InvariantCulture)));
        Row(sb, "Warm pass hit rate", results.Select(r => HitRate(r.CacheAfterCold, r.CacheAfterWarm)));
        Line(sb);
        Line(sb, "Cold-pass hits happen when different queries normalise to the same text. On the warm pass every query is served from the in-memory tier, so the provider isn't called at all.");
        Line(sb);

        Line(sb, "## Personalisation");
        Line(sb);
        Line(sb, "Two customers use the same ambiguous words to mean different products. Each customer confirms each phrase "
                 + "3 times, then the phrase is matched again, both as written and reworded as an order (\"2 boxes of … please\").");
        Line(sb);
        Header(sb, ["Customer", "Phrase", "Means", .. results.Select(r => $"{r.Provider} before → after")]);
        foreach (var group in first.History.Select((h, i) => (h, i)).Where(x => x.h.Phrasing == x.h.Case.Query))
        {
            var c = group.h.Case;
            Row(sb, c.CustomerId, [$"\"{c.Query}\"", $"`{c.ExpectedSku}`",
                .. results.Select(r => $"{Mark(r.History[group.i].CorrectBefore)} → {Mark(r.History[group.i].CorrectAfter)}")]);
        }

        Line(sb);

        Line(sb, "The boost is capped (0.25 for the phrase plus 0.05 for the product), so history can't push a weak match past a much stronger one. "
                 + "Any ❌ → ❌ row is a case where the customer's product is too far from their wording for that cap. Those phrases stay in NeedsReview rather than being learned blindly.");
        Line(sb);

        Line(sb, "## Top-1 misses");
        Line(sb);
        foreach (var r in results)
        {
            var misses = r.Cold.Where(o => o.InCatalogue && !o.Top1Correct).ToList();
            Line(sb, $"### {r.Provider}: {misses.Count} misses");
            Line(sb);
            Header(sb, ["Query", "Kind", "Expected", "Got", "Score", "Rank of expected", "Band"]);
            foreach (var o in misses.Take(MaxMissesListed))
            {
                Row(sb, o.Query.Query, [o.Query.Kind, $"`{o.Query.ExpectedSku}`", $"`{o.TopSku}`", F(o.TopScore),
                    o.Rank?.ToString(CultureInfo.InvariantCulture) ?? ">5", o.Band.ToString()]);
            }

            if (misses.Count > MaxMissesListed)
            {
                Line(sb, $"| … {misses.Count - MaxMissesListed} more | | | | | | |");
            }

            Line(sb);
        }

        Line(sb, "## Caveats");
        Line(sb);
        Line(sb, "- The catalogue and queries are small and synthetic, and the queries were written by the same author as the catalogue. Treat the numbers as a relative comparison, not production accuracy.");
        Line(sb, "- The thresholds are calibrated on the same queries they're reported on. Re-calibrate on held-out real orders before relying on auto-accept.");
        Line(sb, "- OpenAI embeddings are not exactly deterministic, so its numbers can shift by about a point between runs. The local embedder is fully deterministic.");
        Line(sb, "- Latency is measured in-process against local Postgres. OpenAI cold latency includes a network round trip per query.");
        return sb.ToString();
    }

    private static string HitRate(ProductMatcher.Infrastructure.Embeddings.Caching.EmbeddingCacheSnapshot before,
        ProductMatcher.Infrastructure.Embeddings.Caching.EmbeddingCacheSnapshot after)
    {
        var hits = (after.MemoryHits + after.StoreHits) - (before.MemoryHits + before.StoreHits);
        var misses = after.Misses - before.Misses;
        return Pct(hits + misses == 0 ? 0 : (double)hits / (hits + misses));
    }

    private static string Calibrated(ProviderResult r, Func<SweepRow, string> pick) =>
        Analysis.Calibrate(r.Cold).AutoAccept is { } row ? pick(row) : "–";

    private static double Share<T>(IReadOnlyCollection<T> items, Func<T, bool> predicate) =>
        items.Count == 0 ? 0 : (double)items.Count(predicate) / items.Count;

    private static string Mark(bool ok) => ok ? "✅" : "❌";

    private static string Pct(double v) => (v * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string F(double v, int digits = 2) => v.ToString("F" + digits, CultureInfo.InvariantCulture);

    private static string Ms(double v) => v.ToString(v < 10 ? "0.0" : "0", CultureInfo.InvariantCulture) + " ms";

    private static void Header(StringBuilder sb, IEnumerable<string> columns)
    {
        var list = columns.ToList();
        Line(sb, "| " + string.Join(" | ", list) + " |");
        Line(sb, "|" + string.Concat(Enumerable.Repeat(" --- |", list.Count)));
    }

    private static void Row(StringBuilder sb, string first, IEnumerable<string> rest) =>
        Line(sb, "| " + string.Join(" | ", rest.Prepend(first)) + " |");

    private static void Line(StringBuilder sb, string text = "") => sb.Append(text).Append('\n');
}
