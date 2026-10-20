using System.CommandLine;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;

namespace Lld.Content.Cli;

/// <summary>How issues are printed.</summary>
public enum IssueFormat
{
    /// <summary><c>content/x/lesson.md:12: link.broken: message</c>, for people and CI logs.</summary>
    Text,

    /// <summary><c>D:\repo\content\x\lesson.md(12): error link.broken: message</c>, which MSBuild shows as build errors.</summary>
    MSBuild,
}

/// <summary>The command tree of <c>lld-content</c>. Every command reads the repository, validates it and stops on any issue.</summary>
internal static class ContentCli
{
    public const int Success = 0;
    public const int Failure = 1;

    public static Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        var repo = new Option<DirectoryInfo>("--repo")
        {
            Description = "Repository root, the folder that contains content/. Default: the current folder.",
            DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory()),
            Recursive = true,
        };
        var format = new Option<IssueFormat>("--format")
        {
            Description = "How to print issues: Text (default) or MSBuild (build errors).",
            DefaultValueFactory = _ => IssueFormat.Text,
            Recursive = true,
        };

        var validate = new Command("validate", "Check content/ against the authoring contract; exit code 1 on any issue.");
        validate.SetAction(parse =>
        {
            var result = Build(parse.GetValue(repo)!, parse.GetValue(format), error);
            if (result.Pack is { } pack)
            {
                output.WriteLine($"Content is valid: {Describe(pack)}.");
                return Success;
            }

            return Failure;
        });

        var packOut = OutOption("Folder to write content-pack.json and content-version.txt to.");
        var pack = new Command("pack", "Validate, then write the pack for the app (only rewritten when the content changed).") { packOut };
        pack.SetAction(async (parse, ct) =>
        {
            var result = Build(parse.GetValue(repo)!, parse.GetValue(format), error);
            if (result.Pack is null)
            {
                return Failure;
            }

            var written = await PackWriter.WriteAsync(result.Pack, parse.GetValue(packOut)!, ct);
            output.WriteLine(written
                ? $"Packed {Describe(result.Pack)} -> {parse.GetValue(packOut)!.FullName}"
                : $"Content pack is up to date ({Short(result.Pack.Version)}).");
            return Success;
        });

        var snapshotOut = OutOption("Folder to write the API responses to (used by the Playwright tests).");
        var searchRequests = new Option<FileInfo?>("--search-requests")
        {
            Description = "A JSON list of search requests whose /api/search responses are written too (spec 009).",
        };
        var snapshot = new Command("snapshot", "Validate, then write every /api/content response as a static JSON file.")
        {
            snapshotOut,
            searchRequests,
        };
        snapshot.SetAction(async (parse, ct) =>
        {
            var result = Build(parse.GetValue(repo)!, parse.GetValue(format), error);
            if (result.Pack is null)
            {
                return Failure;
            }

            var files = await ApiSnapshotWriter.WriteAsync(new InMemoryContentCatalog(result.Pack), parse.GetValue(snapshotOut)!, ct);
            if (parse.GetValue(searchRequests) is { } requests)
            {
                files += await SearchSnapshotWriter.WriteAsync(result.Pack, requests, parse.GetValue(snapshotOut)!, ct);
            }

            output.WriteLine($"Wrote {files} API responses -> {parse.GetValue(snapshotOut)!.FullName}");
            return Success;
        });

        var root = new RootCommand("LLD Guide content tool: validate and pack content/.") { repo, format, validate, pack, snapshot };
        var invocation = new InvocationConfiguration { Output = output, Error = error };
        return root.Parse(args).InvokeAsync(invocation);
    }

    private static Option<DirectoryInfo> OutOption(string description) =>
        new("--out") { Description = description, Required = true };

    private static ContentBuildResult Build(DirectoryInfo repo, IssueFormat format, TextWriter error)
    {
        var result = new ContentPipeline(new PhysicalContentFiles(repo.FullName), new ContentOptions()).Build();
        foreach (var issue in result.Issues)
        {
            error.WriteLine(Format(issue, repo, format));
        }

        if (!result.Succeeded)
        {
            error.WriteLine($"{result.Issues.Count} content issue(s); nothing was written.");
        }

        return result;
    }

    internal static string Format(ContentIssue issue, DirectoryInfo repo, IssueFormat format)
    {
        if (format == IssueFormat.Text)
        {
            return issue.ToString();
        }

        var path = Path.GetFullPath(Path.Combine(repo.FullName, issue.Path.Replace('/', Path.DirectorySeparatorChar)));
        var origin = issue.Line is { } line ? $"{path}({line})" : path;
        return $"{origin}: error {issue.Code}: {issue.Message}";
    }

    private static string Describe(ContentPack pack) =>
        $"{pack.Modules.Count} module(s), {pack.Modules.Sum(m => m.Lessons.Count)} lesson(s), version {Short(pack.Version)}";

    private static string Short(string version) => version[..Math.Min(12, version.Length)];
}
