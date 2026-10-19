using FluentAssertions;
using Lld.Domain.Progress;

namespace Lld.Progress.Tests;

/// <summary>
/// Behaviour every <see cref="IProgressStore"/> must have. The in-memory, device (SQLite) and API (EF Core) stores each
/// derive from this class, so a learner's progress means the same thing wherever it is kept (Liskov).
/// </summary>
public abstract class ProgressStoreContract
{
    protected static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
    protected static readonly ProgressOwner Ada = ProgressOwner.User("user-ada");
    protected static readonly ProgressOwner Grace = ProgressOwner.User("user-grace");

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract Task<IProgressStore> CreateStoreAsync();

    [Fact]
    public async Task Get_IsEmptyForAnOwnerWithNoProgress()
    {
        var store = await CreateStoreAsync();

        (await store.GetAsync(Ada, Ct)).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SaveLessons_KeepsEveryField()
    {
        var store = await CreateStoreAsync();
        var done = Full("srp");

        await store.SaveLessonsAsync(Ada, [done, Opened("ocp", 1)], Ct);

        (await store.GetAsync(Ada, Ct)).Lessons.Should().BeEquivalentTo([Opened("ocp", 1), done], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task SaveLessons_IgnoresAnOlderEdit()
    {
        var store = await CreateStoreAsync();
        await store.SaveLessonsAsync(Ada, [Full("srp", minutes: 10)], Ct);

        await store.SaveLessonsAsync(Ada, [Opened("srp", minutes: 2)], Ct);

        (await store.GetAsync(Ada, Ct)).Lessons.Single().Status.Should().Be(LessonStatus.Done);
    }

    [Fact]
    public async Task SaveLessons_AppliesANewerEdit()
    {
        var store = await CreateStoreAsync();
        await store.SaveLessonsAsync(Ada, [Full("srp", minutes: 1)], Ct);
        var reset = Opened("srp", minutes: 5) with { Status = LessonStatus.NotStarted, LastReadAt = null };

        await store.SaveLessonsAsync(Ada, [reset], Ct);

        (await store.GetAsync(Ada, Ct)).Lessons.Should().BeEquivalentTo([reset]);
    }

    [Fact]
    public async Task Owners_NeverSeeEachOthersProgress()
    {
        var store = await CreateStoreAsync();
        await store.SaveLessonsAsync(Ada, [Full("srp")], Ct);
        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 19), Ct);

        await store.SaveLessonsAsync(Grace, [Opened("srp", 9)], Ct);

        (await store.GetAsync(Ada, Ct)).Lessons.Single().Status.Should().Be(LessonStatus.Done);
        (await store.GetAsync(Grace, Ct)).ActivityDays.Should().BeEmpty();
    }

    [Fact]
    public async Task ActivityDays_AreKeptOnceAndInOrder()
    {
        var store = await CreateStoreAsync();

        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 19), Ct);
        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 17), Ct);
        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 19), Ct);

        (await store.GetAsync(Ada, Ct)).ActivityDays.Should().Equal(new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 19));
    }

    [Fact]
    public async Task Merge_CombinesWithWhatIsStoredAndReturnsTheResult()
    {
        var store = await CreateStoreAsync();
        await store.SaveLessonsAsync(Ada, [Opened("srp", 1) with { ReadSections = ["rule"] }, Opened("ocp", 1)], Ct);
        await store.AddActivityDayAsync(Ada, new DateOnly(2026, 10, 18), Ct);
        var guest = new ProgressSnapshot(
            [Opened("srp", 3) with { ReadSections = ["smells"] }, Full("lsp")], [new DateOnly(2026, 10, 19)]);

        var merged = await store.MergeAsync(Ada, guest, Ct);

        merged.Should().BeEquivalentTo(await store.GetAsync(Ada, Ct));
        merged.Lessons.Select(l => l.LessonId).Should().Equal("lsp", "ocp", "srp");
        merged.Lessons.Single(l => l.LessonId == "srp").ReadSections.Should().Equal("rule", "smells");
        merged.ActivityDays.Should().HaveCount(2);
    }

    protected static LessonProgress Opened(string lessonId, int minutes = 0) =>
        new(lessonId, LessonStatus.InProgress, null, null, [], null, T0.AddMinutes(minutes), null, T0.AddMinutes(minutes), "device-a");

    /// <summary>A record with every optional field filled, to prove nothing is lost on the way to storage.</summary>
    protected static LessonProgress Full(string lessonId, int minutes = 0) =>
        Opened(lessonId, minutes) with
        {
            Status = LessonStatus.Done,
            DoneBy = DoneBy.Manual,
            CompletedAt = T0.AddMinutes(minutes).AddSeconds(30),
            ReadSections = ["rule", "picture", "smells"],
            Position = new ReadingPosition("smells", 120),
            LastRevisedAt = T0.AddMinutes(minutes).AddSeconds(45),
        };
}

public sealed class InMemoryProgressStoreTests : ProgressStoreContract
{
    protected override Task<IProgressStore> CreateStoreAsync() => Task.FromResult<IProgressStore>(new InMemoryProgressStore());
}
