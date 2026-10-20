namespace Lld.Samples.Uml.Multiplicity;

/// <summary>1 library holds 0..* books.</summary>
public sealed class Library
{
    private readonly List<Book> _books = [];

    public IReadOnlyList<Book> Books => _books;

    public void Add(Book book) => _books.Add(book);
}

/// <summary>A book has 1..* authors and is on loan to 0..1 members. It knows them; they don't know it (one-way arrows).</summary>
public sealed class Book
{
    public Book(string title, IReadOnlyList<Author> authors)
    {
        if (authors.Count == 0)
        {
            throw new ArgumentException("A book has at least one author.", nameof(authors));
        }

        Title = title;
        Authors = authors;
    }

    public string Title { get; }

    public IReadOnlyList<Author> Authors { get; }

    public Member? Borrower { get; private set; }

    public void Lend(Member member)
    {
        if (Borrower is not null)
        {
            throw new InvalidOperationException($"'{Title}' is already on loan.");
        }

        Borrower = member;
    }

    public void Return() => Borrower = null;
}

public sealed record Author(string Name);

public sealed class Member(string name)
{
    public string Name { get; } = name;
}
