using System.Text.Json;

namespace Lld.Samples.OopSolid.Inheritance.After;

public sealed record Contact(string Name, string Email);

public sealed record ImportResult(IReadOnlyList<Contact> Contacts, IReadOnlyList<string> Errors);

/// <summary>
/// Template Method: the base class owns the algorithm (split into lines, read each one, check it, normalise it,
/// collect errors) and leaves exactly one step to subclasses: reading one line of their format.
/// </summary>
public abstract class ContactImporter
{
    public ImportResult Import(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var contacts = new List<Contact>();
        var errors = new List<string>();
        var number = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            number++;
            if (ReadLine(line) is { } contact && contact.Email.Contains('@', StringComparison.Ordinal))
            {
                contacts.Add(Normalise(contact));
            }
            else
            {
                errors.Add($"Line {number}: expected {ExpectedFormat}.");
            }
        }

        return new ImportResult(contacts, errors);
    }

    /// <summary>What one line looks like, for error messages.</summary>
    protected abstract string ExpectedFormat { get; }

    /// <summary>The one step that differs: one line of this format, or <c>null</c> when it can't be read.</summary>
    protected abstract Contact? ReadLine(string line);

    private static Contact Normalise(Contact contact) =>
        new(contact.Name.Trim(), contact.Email.Trim().ToLowerInvariant());
}

public sealed class CsvContactImporter : ContactImporter
{
    protected override string ExpectedFormat => "'name,email'";

    protected override Contact? ReadLine(string line) =>
        line.Split(',') is [var name, var email] ? new Contact(name, email) : null;
}

public sealed class JsonLinesContactImporter : ContactImporter
{
    protected override string ExpectedFormat => "{\"Name\": ..., \"Email\": ...}";

    protected override Contact? ReadLine(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<Contact>(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
