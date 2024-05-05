using RsdbMerger.Core.Services;

RsdbChangelogService changelogService = new(args[0], args[1]);
changelogService.CreateChangelogs();
