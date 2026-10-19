using System.Text.Json;

namespace Lld.Samples.OopSolid.Inheritance.Before;

public sealed record Contact(string Name, string Email);

public sealed record ImportResult(IReadOnlyList<Contact> Contacts, IReadOnlyList<string> Errors);

/// <summary>The import algorithm, written for CSV.</summary>
public static class CsvContactImporter
{
    public static ImportResult Import(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var contacts = new List<Contact>();
        var errors = new List<string>();
        var number = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            number++;
            var parts = line.Split(',');
            if (parts.Length != 2 || !parts[1].Contains('@', StringComparison.Ordinal))
            {
                errors.Add($"Line {number}: expected 'name,email'.");
                continue;
            }

            contacts.Add(new Contact(parts[0].Trim(), parts[1].Trim().ToLowerInvariant()));
        }

        return new ImportResult(contacts, errors);
    }
}

/// <summary>The same algorithm copied for JSON lines; the e-mail normalisation didn't make it across.</summary>
public static class JsonLinesContactImporter
{
    public static ImportResult Import(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var contacts = new List<Contact>();
        var errors = new List<string>();
        var number = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            number++;
            var contact = TryRead(line);
            if (contact is null || !contact.Email.Contains('@', StringComparison.Ordinal))
            {
                errors.Add($"Line {number}: expected {{\"Name\": ..., \"Email\": ...}}.");
                continue;
            }

            contacts.Add(contact);
        }

        return new ImportResult(contacts, errors);
    }

    private static Contact? TryRead(string line)
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
