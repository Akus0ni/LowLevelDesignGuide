using Lld.Content.Authoring.Files;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// A small repository whose content passes every rule. Tests break exactly one thing with <see cref="Set"/> or
/// <see cref="Remove"/> and check that exactly the expected issue appears. <c>parking-lot</c> is linked from nowhere,
/// so removing its files only triggers the "missing file" rules.
/// </summary>
internal sealed class ValidRepo
{
    public const string StartModule = "content/start/module.yml";
    public const string CaseStudiesModule = "content/case-studies/module.yml";
    public const string HowToUse = "content/start/how-to-use/lesson.md";
    public const string HowToUseSheet = "content/start/how-to-use/cheatsheet.md";
    public const string Auth = "content/case-studies/auth/lesson.md";
    public const string AuthSheet = "content/case-studies/auth/cheatsheet.md";
    public const string Parking = "content/case-studies/parking-lot/lesson.md";
    public const string ParkingSheet = "content/case-studies/parking-lot/cheatsheet.md";
    public const string SourceFile = "src/Lld.Domain/Access/AccessRequest.cs";

    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal)
    {
        [StartModule] = "title: Start here\norder: 0\nsummary: How the guide works.\n",
        [CaseStudiesModule] = "title: Case studies\norder: 2\nsummary: Real designs, start to finish.\n",
        [HowToUse] = """
            ---
            id: how-to-use
            title: How to use LLD Guide
            order: 1
            summary: Modules, lessons and cheat sheets.
            tags: [guide]
            ---
            # How to use LLD Guide

            ## Modules

            Start with the [Auth case study](../../case-studies/auth/lesson.md#token-rotation) and keep its
            [cheat sheet](../../case-studies/auth/cheatsheet.md) open. Back to [modules](#modules).
            """,
        [HowToUseSheet] = "---\ntitle: \"Cheat sheet: How to use\"\n---\n# How to use\n\n- Read, then revise.\n",
        [Auth] = """
            ---
            id: case-study-auth
            title: "Case Study 1: Authentication"
            order: 1
            summary: The app's own sign-in, class by class.
            tags: [state, strategy]
            source: self-referential
            ---
            # Case Study 1: Authentication

            ## Token rotation

            See [AccessRequest.cs][request] and [the guide](../../start/how-to-use/lesson.md).

            ```mermaid
            stateDiagram-v2
              Pending --> Approved
            ```

            ```csharp
            public sealed class AccessRequest;
            ```

            [request]: ../../../src/Lld.Domain/Access/AccessRequest.cs
            """,
        [AuthSheet] = "---\ntitle: \"Cheat sheet: Authentication\"\n---\n# Auth\n\n## Rules\n\n- Rotate refresh tokens.\n",
        [Parking] = "---\nid: case-study-parking\ntitle: Parking lot\norder: 2\nsummary: Classic.\ntags: []\nsource: classic\n---\n# Parking lot\n",
        [ParkingSheet] = "---\ntitle: \"Cheat sheet: Parking lot\"\n---\n# Parking lot\n",
        [SourceFile] = "public sealed class AccessRequest;",
    };

    public ValidRepo Set(string path, string text)
    {
        _files[path] = text;
        return this;
    }

    public ValidRepo Remove(string path)
    {
        _files.Remove(path);
        return this;
    }

    /// <summary>Replaces text inside one file (the text must be there, so a typo in a test fails loudly).</summary>
    public ValidRepo Replace(string path, string oldText, string newText)
    {
        // The raw strings above take this file's line endings (CRLF in a Windows checkout); tests write "\n".
        var text = _files[path].ReplaceLineEndings("\n");
        if (!text.Contains(oldText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"'{oldText}' is not in {path}.");
        }

        _files[path] = text.Replace(oldText, newText, StringComparison.Ordinal);
        return this;
    }

    /// <summary>Writes the repository to a real folder (for the command-line tool).</summary>
    public void WriteTo(string root)
    {
        foreach (var (path, text) in _files)
        {
            var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }
    }

    public InMemoryContentFiles Files()
    {
        var files = new InMemoryContentFiles();
        foreach (var (path, text) in _files)
        {
            files.Add(path, text);
        }

        return files;
    }
}
