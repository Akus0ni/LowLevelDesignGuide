namespace Lld.Samples.Patterns.Enterprise.NullObject.Modern;

/// <summary>
/// .NET ships null objects: <see cref="TextWriter.Null"/>, <see cref="Stream.Null"/>, <c>NullLogger&lt;T&gt;.Instance</c>,
/// <see cref="Enumerable.Empty{TResult}"/>. Default to them instead of <c>null</c>, and let nullable reference types
/// flag the places that could still be null.
/// </summary>
public sealed class Receipts(TextWriter? output = null)
{
    private readonly TextWriter _output = output ?? TextWriter.Null;

    public void Print(decimal total) => _output.Write($"Total: {total}");
}
