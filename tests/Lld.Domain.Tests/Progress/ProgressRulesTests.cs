using FluentAssertions;
using Lld.Domain.Progress;
using static Lld.Domain.Tests.Progress.ProgressBuilder;

namespace Lld.Domain.Tests.Progress;

public sealed class ProgressRulesTests
{
    [Fact]
    public void Newer_KeepsTheLaterEdit()
    {
        var stored = Opened(minutes: 5);
        var incoming = Done(minutes: 1);

        ProgressRules.Newer(stored, incoming).Should().BeSameAs(stored);
    }

    [Fact]
    public void Newer_LetsTheIncomingRecordWinATie()
    {
        var stored = Opened(minutes: 5);
        var incoming = Done(minutes: 5);

        ProgressRules.Newer(stored, incoming).Should().BeSameAs(incoming);
    }

    [Theory]
    [InlineData(LessonStatus.NotStarted, LessonStatus.InProgress, LessonStatus.InProgress)]
    [InlineData(LessonStatus.InProgress, LessonStatus.NotStarted, LessonStatus.InProgress)]
    [InlineData(LessonStatus.InProgress, LessonStatus.Done, LessonStatus.Done)]
    [InlineData(LessonStatus.Done, LessonStatus.NotStarted, LessonStatus.Done)]
    [InlineData(LessonStatus.NotStarted, LessonStatus.NotStarted, LessonStatus.NotStarted)]
    public void Merge_KeepsTheFurthestStatus(LessonStatus first, LessonStatus second, LessonStatus expected)
    {
        var merged = ProgressRules.Merge(WithStatus(first, minutes: 1), WithStatus(second, minutes: 2));

        merged.Status.Should().Be(expected);
    }

    [Fact]
    public void Merge_TakesTheCompletionFromTheDoneSide()
    {
        var merged = ProgressRules.Merge(Done(minutes: 1, by: DoneBy.Manual), Opened(minutes: 9));

        merged.Should().Match<LessonProgress>(m => m.DoneBy == DoneBy.Manual && m.CompletedAt == At(1));
    }

    [Fact]
    public void Merge_TakesTheLaterFinishWhenBothAreDone()
    {
        var merged = ProgressRules.Merge(Done(minutes: 1, by: DoneBy.Manual), Done(minutes: 4, by: DoneBy.Auto));

        merged.Should().Match<LessonProgress>(m => m.DoneBy == DoneBy.Auto && m.CompletedAt == At(4));
    }

    [Fact]
    public void Merge_CombinesTheReadSections()
    {
        var merged = ProgressRules.Merge(
            Opened() with { ReadSections = ["rule", "picture"] },
            Opened() with { ReadSections = ["picture", "smells"] });

        merged.ReadSections.Should().Equal("rule", "picture", "smells");
    }

    [Fact]
    public void Merge_KeepsThePositionOfTheMoreRecentRead()
    {
        var merged = ProgressRules.Merge(
            Opened(minutes: 7) with { Position = new ReadingPosition("smells", 40) },
            Opened(minutes: 2) with { Position = new ReadingPosition("rule", 0) });

        merged.Position.Should().Be(new ReadingPosition("smells", 40));
    }

    [Fact]
    public void Merge_FallsBackToTheOtherPositionWhenTheRecentReadHasNone()
    {
        var merged = ProgressRules.Merge(Opened(minutes: 7), Opened(minutes: 2) with { Position = new ReadingPosition("rule", 0) });

        merged.Position.Should().Be(new ReadingPosition("rule", 0));
    }

    [Fact]
    public void Merge_KeepsTheLatestDatesAndTheLastEditor()
    {
        var merged = ProgressRules.Merge(
            Opened(minutes: 3) with { LastRevisedAt = At(8), DeviceId = "phone" },
            Opened(minutes: 6) with { LastRevisedAt = null, DeviceId = "laptop" });

        merged.Should().Match<LessonProgress>(m =>
            m.LastReadAt == At(6) && m.LastRevisedAt == At(8) && m.UpdatedAt == At(6) && m.DeviceId == "laptop");
    }

    [Fact]
    public void Merge_RefusesDifferentLessons()
    {
        var merge = () => ProgressRules.Merge(Opened("srp"), Opened("ocp"));

        merge.Should().Throw<ArgumentException>().WithMessage("*'srp' vs 'ocp'*");
    }

    [Fact]
    public void MergeSnapshots_MergesSharedLessonsAndKeepsTheRest()
    {
        var guest = new ProgressSnapshot([Done("srp", minutes: 2), Opened("ocp")], [new DateOnly(2026, 10, 18)]);
        var user = new ProgressSnapshot([Opened("srp", minutes: 1), Opened("lsp")], [new DateOnly(2026, 10, 18), new DateOnly(2026, 10, 19)]);

        var merged = ProgressRules.Merge(user, guest);

        merged.Lessons.Select(l => (l.LessonId, l.Status)).Should().Equal(
            ("lsp", LessonStatus.InProgress), ("ocp", LessonStatus.InProgress), ("srp", LessonStatus.Done));
        merged.ActivityDays.Should().Equal(new DateOnly(2026, 10, 18), new DateOnly(2026, 10, 19));
    }

    private static LessonProgress WithStatus(LessonStatus status, int minutes) =>
        status == LessonStatus.Done ? Done(minutes: minutes) : Opened(minutes: minutes) with { Status = status };
}
