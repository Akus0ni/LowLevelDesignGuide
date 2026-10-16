namespace Lld.App.Data.Schema;

/// <summary>Step 2 (spec 003 §3.8): code-callout terms per lesson. Existing rows get none until the next pack loads.</summary>
public sealed class V2LessonTerms : ISchemaMigration
{
    public int Version => 2;

    public string Name => "Lesson terms";

    public string Sql => "ALTER TABLE Lessons ADD COLUMN TermsJson TEXT NOT NULL DEFAULT '[]';";
}
