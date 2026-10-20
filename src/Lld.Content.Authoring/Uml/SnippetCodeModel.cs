using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lld.Content.Authoring.Uml;

/// <summary>A 1-based, inclusive line range in the quoted code.</summary>
public sealed record CodeLines(int First, int Last)
{
    public IEnumerable<int> All => Enumerable.Range(First, Last - First + 1);
}

/// <summary>A name used in a declaration, and the line it is on.</summary>
public sealed record CodeUse(string Name, int Line);

/// <summary>
/// A field, property or record parameter: the type names it holds (<c>List&lt;OrderLine&gt;</c> holds OrderLine) and
/// whether it is a collection.
/// </summary>
public sealed record CodeMember(IReadOnlyList<string> TypeNames, bool IsCollection, int Line);

/// <param name="Bases">The base list: base class and interfaces, by plain name.</param>
/// <param name="Mentions">Every other type name used inside the declaration (parameters, locals, <c>new</c>, calls).</param>
public sealed record CodeType(
    string Name,
    CodeLines Lines,
    IReadOnlyList<CodeUse> Bases,
    IReadOnlyList<CodeMember> Members,
    IReadOnlyList<CodeUse> Mentions)
{
    public IEnumerable<CodeMember> MembersOfType(string name) =>
        Members.Where(m => m.TypeNames.Contains(name, StringComparer.Ordinal));
}

/// <summary>
/// What the Code ↔ UML check needs to know about a quoted snippet (spec 008 §3.4): its types with their lines, base
/// lists, members and the type names they mention, and enum members. Built from the same text the reader shows, so
/// every line number matches what the learner sees. Syntax only (no compilation), like the snippet finder.
/// </summary>
public sealed class SnippetCodeModel
{
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    /// <summary>Generic types whose type arguments are what a member really holds.</summary>
    private static readonly HashSet<string> Collections = new(StringComparer.Ordinal)
    {
        "List", "IList", "IReadOnlyList", "IEnumerable", "ICollection", "IReadOnlyCollection", "HashSet", "ISet",
        "IReadOnlySet", "Dictionary", "IDictionary", "IReadOnlyDictionary", "Queue", "Stack", "LinkedList",
        "ImmutableArray", "ImmutableList", "SortedSet", "SortedDictionary", "ObservableCollection", "Collection",
    };

    private readonly Dictionary<string, CodeType> _types;
    private readonly Dictionary<string, int> _enumMembers;

    private SnippetCodeModel(Dictionary<string, CodeType> types, Dictionary<string, int> enumMembers)
    {
        _types = types;
        _enumMembers = enumMembers;
    }

    public IReadOnlyCollection<CodeType> Types => _types.Values;

    public CodeType? Type(string name) => _types.GetValueOrDefault(name);

    /// <summary>1-based line of an enum member with that name, in any enum of the snippet.</summary>
    public int? EnumMember(string name) => _enumMembers.TryGetValue(name, out var line) ? line : null;

    public static SnippetCodeModel Of(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var tree = CSharpSyntaxTree.ParseText(code, ParseOptions);
        var root = tree.GetCompilationUnitRoot();
        var types = new Dictionary<string, CodeType>(StringComparer.Ordinal);
        var enumMembers = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            types.TryAdd(declaration.Identifier.ValueText, TypeOf(declaration));
            if (declaration is EnumDeclarationSyntax enumeration)
            {
                foreach (var member in enumeration.Members)
                {
                    enumMembers.TryAdd(member.Identifier.ValueText, LineOf(member));
                }
            }
        }

        return new SnippetCodeModel(types, enumMembers);
    }

    private static CodeType TypeOf(BaseTypeDeclarationSyntax declaration)
    {
        var span = declaration.GetLocation().GetLineSpan();
        var lines = new CodeLines(span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1);
        var bases = declaration.BaseList?.Types.Select(b => new CodeUse(PlainName(b.Type), LineOf(b))).ToList() ?? [];
        var members = new List<CodeMember>();
        if (declaration is TypeDeclarationSyntax type)
        {
            members.AddRange(type.Members.SelectMany(MembersOf));
            if (type is RecordDeclarationSyntax { ParameterList: { } parameters })
            {
                members.AddRange(parameters.Parameters.Where(p => p.Type is not null).Select(p => Member(p.Type!, LineOf(p))));
            }
        }

        var own = declaration.Identifier.ValueText;
        var mentions = declaration.DescendantNodes()
            .Where(n => n is IdentifierNameSyntax or GenericNameSyntax)
            .Select(n => new CodeUse(((SimpleNameSyntax)n).Identifier.ValueText, LineOf(n)))
            .Where(u => u.Name != own)
            .ToList();
        return new CodeType(own, lines, bases, members, mentions);
    }

    private static IEnumerable<CodeMember> MembersOf(MemberDeclarationSyntax member) => member switch
    {
        FieldDeclarationSyntax field => [Member(field.Declaration.Type, LineOf(field))],
        PropertyDeclarationSyntax property => [Member(property.Type, LineOf(property))],
        EventFieldDeclarationSyntax eventField => [Member(eventField.Declaration.Type, LineOf(eventField))],
        _ => [],
    };

    private static CodeMember Member(TypeSyntax type, int line)
    {
        var names = new List<string>();
        var isCollection = Collect(type, names);
        return new CodeMember(names, isCollection, line);
    }

    /// <summary>The type names a member type holds; true when it is a collection or array of them.</summary>
    private static bool Collect(TypeSyntax type, List<string> names)
    {
        switch (type)
        {
            case NullableTypeSyntax nullable:
                return Collect(nullable.ElementType, names);
            case ArrayTypeSyntax array:
                Collect(array.ElementType, names);
                return true;
            case QualifiedNameSyntax qualified:
                return Collect(qualified.Right, names);
            case AliasQualifiedNameSyntax alias:
                return Collect(alias.Name, names);
            case GenericNameSyntax generic when Collections.Contains(generic.Identifier.ValueText):
                // A dictionary holds its values; every other collection holds its single argument.
                Collect(generic.TypeArgumentList.Arguments[^1], names);
                return true;
            case GenericNameSyntax generic:
                names.Add(generic.Identifier.ValueText);
                foreach (var argument in generic.TypeArgumentList.Arguments)
                {
                    Collect(argument, names);
                }

                return false;
            case SimpleNameSyntax simple:
                names.Add(simple.Identifier.ValueText);
                return false;
            default:
                return false;
        }
    }

    private static string PlainName(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax qualified => PlainName(qualified.Right),
        AliasQualifiedNameSyntax alias => PlainName(alias.Name),
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => type.ToString(),
    };

    private static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
