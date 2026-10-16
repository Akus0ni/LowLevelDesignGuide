namespace Lld.Content.Authoring.Validation;

/// <summary>
/// One validation rule. Each rule is its own class, so adding a rule never changes another one (Open/Closed);
/// <see cref="ContentValidator"/> just runs them all.
/// </summary>
public interface IContentRule
{
    IEnumerable<ContentIssue> Check(ContentContext context);
}
