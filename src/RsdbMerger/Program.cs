using RsdbMerger.Core.Services;
using System.Diagnostics;

Stopwatch watch = Stopwatch.StartNew();

for (int i = 0; i < (args.Length - 1); i++) {
    RsdbChangelogService changelogServiceM1 = new(args[i], args[i] += ".Changelog");
    await changelogServiceM1.CreateChangelogsAsync();
}

RsdbMergerService mergerService = new(args.AsSpan()[..^1], args[^1]);
await mergerService.MergeAsync();

watch.Stop();
Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
