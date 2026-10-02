using ProductMatcher.Benchmark;

var root = FindRepoRoot();
var providers = ArgValue("--providers")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? (Environment.GetEnvironmentVariable("OPENAI_API_KEY") is { Length: > 0 } ? ["Local", "OpenAI"] : ["Local"]);

var settings = new BenchmarkSettings(
    ArgValue("--connection") ?? "Host=localhost;Port=5434;Database=product_matcher_bench;Username=matcher;Password=matcher",
    Path.Combine(root, "data", "sample-catalogue.csv"),
    Path.Combine(root, "data", "benchmark-queries.csv"),
    Path.Combine(root, "data", "benchmark-history.csv"),
    Path.Combine(root, "src", "ProductMatcher.Api", "appsettings.json"),
    ArgValue("--output") ?? Path.Combine(root, "docs", "benchmark.md"));

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var runner = new BenchmarkRunner(settings);
var results = new List<ProviderResult>();
foreach (var provider in providers)
{
    results.Add(await runner.RunAsync(provider, cts.Token));
}

await File.WriteAllTextAsync(settings.OutputPath, ReportWriter.Render(results, DateTimeOffset.Now), cts.Token);

foreach (var r in results)
{
    var accuracy = Analysis.Accuracy(r.Cold);
    var calibration = Analysis.Calibrate(r.Cold);
    Console.WriteLine(
        $"{r.Provider,-7} top1 {accuracy.Top1:P1}  hit@5 {accuracy.HitAt5:P1}  mrr {accuracy.Mrr:F3}  " +
        $"recommended auto {calibration.AutoAccept?.AutoAcceptScore:F2} margin {calibration.AutoAccept?.MinMargin:F2} " +
        $"(precision {calibration.AutoAccept?.Precision:P1}, coverage {calibration.AutoAccept?.Coverage:P1})  review {calibration.ReviewScore:F3}");
}

Console.WriteLine($"Report written to {settings.OutputPath}");
return 0;

string? ArgValue(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "ProductMatcher.slnx")))
        {
            return dir.FullName;
        }
    }

    throw new InvalidOperationException("Run from inside the repository; ProductMatcher.slnx not found.");
}
