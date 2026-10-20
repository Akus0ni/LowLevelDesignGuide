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

    public const string ConceptModule = "content/oop-solid/module.yml";
    public const string Srp = "content/oop-solid/srp/lesson.md";
    public const string SrpSheet = "content/oop-solid/srp/cheatsheet.md";
    public const string Ocp = "content/oop-solid/ocp/lesson.md";
    public const string OcpSheet = "content/oop-solid/ocp/cheatsheet.md";
    public const string Sample = "samples/Srp.cs";

    /// <summary>
    /// Adds a valid concept module (spec 004): snippets, a Before / After pair, callouts, interview questions and a map.
    /// </summary>
    public ValidRepo WithConceptModule()
    {
        Set(ConceptModule, """
            title: OOP & SOLID
            order: 1
            summary: Principles.
            template: concept
            mnemonics: ["SO: srp, ocp"]
            map: |
              flowchart LR
                srp["SRP"] -->|"leads to"| ocp["OCP"]
            """);
        Set(Sample, """
            namespace Samples;

            public sealed class Report
            {
                public string Render() => "report";

                public void Save() { }
            }

            public sealed class ReportRenderer
            {
                public string Render() => "report";
            }
            """);
        Set(Srp, $"""
            ---
            id: srp
            title: SRP
            order: 1
            summary: One reason to change.
            tags: [solid]
            icon: "🎯"
            hook: One reason to change
            ---
            # SRP

            > [!REMEMBER]
            > One job.

            > [!SMELL]
            > A class that renders and saves.

            ```csharp before title="Before" snippet="samples/Srp.cs#Report"
            ```
            ```csharp after title="After" snippet="samples/Srp.cs#ReportRenderer"
            ```

            ```csharp snippet="samples/Srp.cs#Report.Save"
            ```

            {InterviewSection(5)}
            """);
        Set(SrpSheet, "---\ntitle: \"Cheat sheet: SRP\"\n---\n# SRP\n\n> [!REMEMBER]\n> One job.\n\n## In one line\n\nOne reason to change.\n");
        Set(Ocp, "---\nid: ocp\ntitle: OCP\norder: 2\nsummary: Extend, don't edit.\ntags: [solid]\nicon: \"➕\"\nhook: Add, don't edit\n---\n"
            + $"# OCP\n\n> [!REMEMBER]\n> Extend.\n\n{InterviewSection(5)}");
        Set(OcpSheet, "---\ntitle: \"Cheat sheet: OCP\"\n---\n# OCP\n\n> [!REMEMBER]\n> Extend.\n\n## In one line\n\nExtend.\n");
        return this;
    }

    public const string PatternModule = "content/patterns/module.yml";
    public const string Strategy = "content/patterns/strategy/lesson.md";
    public const string StrategySheet = "content/patterns/strategy/cheatsheet.md";
    public const string State = "content/patterns/state/lesson.md";
    public const string PatternSample = "samples/Strategy.cs";

    /// <summary>
    /// Adds a valid pattern module (spec 006): two lessons with every fixed section, a structure diagram, a Before /
    /// After pair, an Overuse stamp and a symbol.
    /// </summary>
    public ValidRepo WithPatternModule()
    {
        Set(PatternModule, """
            title: Patterns
            order: 3
            summary: Named designs.
            template: pattern
            picker:
              start: what
              questions:
                what:
                  ask: "Who changes the behaviour?"
                  answers:
                    - { label: "The caller", pick: [strategy], why: "Pass the algorithm in." }
                    - { label: "The object", next: inside }
                inside:
                  ask: "Does it depend on its state?"
                  answers:
                    - { label: "Yes", pick: [state], why: "Each state is a class." }
                    - { label: "No", pick: [strategy], why: "Still a strategy." }
            lookalikes:
              - patterns: [strategy, state]
                difference: "Strategy is chosen by the caller; State switches itself."
                ask: "Who swaps the behaviour?"
            """);
        Set(PatternSample, """
            namespace Samples;

            public static class Shipping
            {
                public static decimal Cost(string mode) => mode == "air" ? 10m : 2m;
            }

            public interface IShippingCost
            {
                decimal Cost();
            }

            public static class Pricing
            {
                public static decimal Cost(System.Func<decimal> cost) => cost();
            }
            """);
        Set(Strategy, PatternLesson("strategy", "Strategy", 1, "St"));
        Set(StrategySheet, PatternSheet("Strategy"));
        Set(State, PatternLesson("state", "State", 2, "Sa"));
        Set("content/patterns/state/cheatsheet.md", PatternSheet("State"));
        return this;
    }

    private static string PatternLesson(string id, string title, int order, string symbol) => $"""
        ---
        id: {id}
        title: {title}
        order: {order}
        summary: A named design.
        tags: [behavioral]
        icon: "🧭"
        hook: Swap how, keep what
        symbol: {symbol}
        ---
        # {title}

        > [!REMEMBER]
        > Swap how, keep what.

        ## The idea

        Pass the algorithm in.

        ## Structure

        ```mermaid
        classDiagram
          Context --> IStrategy
        ```

        ## See it

        ```csharp before title="Before" snippet="samples/Strategy.cs#Shipping"
        ```
        ```csharp after title="After" snippet="samples/Strategy.cs#IShippingCost"
        ```

        ## Modern C#

        ```csharp snippet="samples/Strategy.cs#Pricing"
        ```

        ## Seen in .NET

        - `IComparer<T>`

        ## Travels with

        - Factory Method picks one.

        > [!OVERUSE]
        > One strategy forever.

        {InterviewSection(5)}
        """;

    private static string PatternSheet(string title) =>
        $"---\ntitle: \"Cheat sheet: {title}\"\n---\n# {title}\n\n> [!REMEMBER]\n> Swap how.\n\n## Use it when\n\nThe algorithm varies.\n";

    public const string DiagramModule = "content/uml/module.yml";
    public const string ClassBasics = "content/uml/class-basics/lesson.md";
    public const string ClassBasicsSheet = "content/uml/class-basics/cheatsheet.md";
    public const string UseCase = "content/uml/use-case-diagram/lesson.md";
    public const string Interviews = "content/uml/uml-in-interviews/lesson.md";
    public const string DiagramSample = "samples/Uml.cs";

    /// <summary>
    /// A UML module (spec 008): a diagram lesson with a Code ↔ UML pair, one with From requirements, and the interview
    /// lesson. Its picker has a title and leaves the interview lesson out (allowed outside pattern modules).
    /// </summary>
    public ValidRepo WithDiagramModule()
    {
        Set(DiagramModule, """
            title: UML
            order: 4
            summary: Diagrams for interviews.
            template: diagram
            picker:
              title: "Which diagram?"
              start: what
              questions:
                what:
                  ask: "What should the diagram show?"
                  answers:
                    - { label: "The parts", pick: [class-basics], why: "Classes and how they connect." }
                    - { label: "Who does what", pick: [use-case-diagram], why: "Actors and their goals." }
            """);
        Set(DiagramSample, """
            namespace Samples;

            public sealed class Order(Customer customer)
            {
                private readonly List<OrderLine> _lines = [];

                public Customer Customer { get; } = customer;

                public void Add(OrderLine line) => _lines.Add(line);
            }

            public sealed class OrderLine;

            public sealed class Customer;
            """);
        Set(ClassBasics, DiagramLesson("class-basics", "Classes and members", 1, """
            ## Notation

            ```mermaid
            classDiagram
              class Order {
                +Add(line)
              }
            ```

            ## Read it

            ```mermaid
            classDiagram
              Order --> Customer
            ```

            ## Code ↔ UML

            ```mermaid code="samples/Uml.cs#Order,OrderLine,Customer"
            classDiagram
              Order *-- OrderLine
              Order --> Customer
            ```
            """));
        Set(ClassBasicsSheet, DiagramSheet("Classes and members"));
        Set(UseCase, DiagramLesson("use-case-diagram", "Use case diagram", 2, """
            ## Notation

            ```mermaid
            flowchart TD
              actor["🧍 Learner"] --> read(["Read a lesson"])
            ```

            ## Read it

            ```mermaid
            flowchart TD
              admin["🧍 Admin"] --> approve(["Approve access"])
            ```

            ## From requirements

            - A learner asks for access.

            ```mermaid
            flowchart TD
              learner["🧍 Learner"] --> ask(["Request access"])
            ```
            """));
        Set("content/uml/use-case-diagram/cheatsheet.md", DiagramSheet("Use case diagram"));
        Set(Interviews, DiagramLesson("uml-in-interviews", "UML in an LLD interview", 3, """
            ## Which diagram for which question

            | Question | Diagram |
            |---|---|
            | Who uses it? | Use case |

            ## Worked example

            ```mermaid
            classDiagram
              Library --> Book
            ```
            """));
        Set("content/uml/uml-in-interviews/cheatsheet.md", DiagramSheet("UML in an LLD interview"));
        return this;
    }

    private static string DiagramLesson(string id, string title, int order, string middle) => $"""
        ---
        id: {id}
        title: {title}
        order: {order}
        summary: A diagram.
        tags: [uml]
        icon: "📐"
        hook: Boxes and arrows
        ---
        # {title}

        > [!REMEMBER]
        > Boxes and arrows.

        ## The idea

        What it shows.

        {middle}

        ## In an interview

        Draw it first.

        > [!MISTAKES]
        > - Arrows the wrong way.

        {InterviewSection(5)}
        """;

    private static string DiagramSheet(string title) =>
        $"---\ntitle: \"Cheat sheet: {title}\"\n---\n# {title}\n\n> [!REMEMBER]\n> Boxes.\n\n## Draw it when\n\nAlways.\n";

    /// <summary>1-based line of the first line in <paramref name="path"/> that contains <paramref name="text"/>.</summary>
    public int LineOf(string path, string text)
    {
        var lines = _files[path].ReplaceLineEndings("\n").Split('\n');
        var index = Array.FindIndex(lines, l => l.Contains(text, StringComparison.Ordinal));
        return index < 0 ? throw new InvalidOperationException($"'{text}' is not in {path}.") : index + 1;
    }

    public static string InterviewSection(int questions) =>
        "## Interview questions\n\n" + string.Concat(Enumerable.Range(1, questions).Select(i => $"### Question {i}?\n\nAnswer {i}.\n\n"));

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
