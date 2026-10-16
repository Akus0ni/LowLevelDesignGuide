using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lld.Content.Authoring.Snippets;

/// <summary>The declarations one selector names: a type, or every overload of one member.</summary>
public sealed record SymbolDeclarations(IReadOnlyList<MemberDeclarationSyntax> Nodes);

/// <summary>
/// Finds declarations by name in a C# syntax tree (no compilation, so it is fast and needs no references).
/// <list type="bullet">
/// <item><c>Type</c>: any class, record, struct, interface, enum or delegate with that name, at any nesting depth.</item>
/// <item><c>Outer.Inner</c>: a nested type. <c>Box&lt;T&gt;</c>: a generic type with one type parameter.</item>
/// <item><c>Type.Member</c>: methods, properties, fields, events, enum members, or constructors (<c>Type.Type</c>);
/// all overloads together.</item>
/// </list>
/// More than one result means the selector is ambiguous.
/// </summary>
public static partial class CSharpSymbolFinder
{
    public static IReadOnlyList<SymbolDeclarations> Find(CompilationUnitSyntax root, string selector)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);
        var parts = Parts(selector);
        var types = TypesMatching(root, parts);
        if (types.Count > 0 || parts.Count < 2)
        {
            return [.. types.Select(type => new SymbolDeclarations([type]))];
        }

        return [.. TypesMatching(root, parts[..^1])
            .OfType<TypeDeclarationSyntax>()
            .Select(owner => MembersNamed(owner, parts[^1].Name))
            .Where(members => members.Count > 0)
            .Select(members => new SymbolDeclarations(members))];
    }

    private sealed record NamePart(string Name, int? Arity);

    private static List<NamePart> Parts(string selector) =>
        [.. PartPattern().Matches(selector).Select(match => new NamePart(
            match.Groups["name"].Value,
            match.Groups["args"].Success ? match.Groups["args"].Value.Count(c => c == ',') + 1 : null))];

    /// <summary>Types whose name is the last part and whose containing types match the parts before it.</summary>
    private static List<MemberDeclarationSyntax> TypesMatching(CompilationUnitSyntax root, IReadOnlyList<NamePart> parts) =>
        [.. root.DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .Where(type => ChainMatches(type, parts))];

    private static bool ChainMatches(MemberDeclarationSyntax type, IReadOnlyList<NamePart> parts)
    {
        SyntaxNode? current = type;
        for (var i = parts.Count - 1; i >= 0; i--)
        {
            if (current is not MemberDeclarationSyntax declaration || !NameMatches(declaration, parts[i]))
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }

    private static bool NameMatches(MemberDeclarationSyntax declaration, NamePart part) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.Text == part.Name && ArityMatches(Arity(type), part),
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.Text == part.Name
            && ArityMatches(@delegate.TypeParameterList?.Parameters.Count ?? 0, part),
        _ => false,
    };

    private static int Arity(BaseTypeDeclarationSyntax type) =>
        (type as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Count ?? 0;

    private static bool ArityMatches(int arity, NamePart part) => part.Arity is null || part.Arity == arity;

    private static List<MemberDeclarationSyntax> MembersNamed(TypeDeclarationSyntax owner, string name) =>
        [.. owner.Members.Where(member => member switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text == name,
            PropertyDeclarationSyntax property => property.Identifier.Text == name,
            EventDeclarationSyntax @event => @event.Identifier.Text == name,
            BaseFieldDeclarationSyntax field => field.Declaration.Variables.Any(v => v.Identifier.Text == name),
            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text == name,
            EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.Text == name,
            _ => false,
        })];

    [GeneratedRegex(@"(?<name>\w+)(<(?<args>[^>]*)>)?", RegexOptions.CultureInvariant)]
    private static partial Regex PartPattern();
}
