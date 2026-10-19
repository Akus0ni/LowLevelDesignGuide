using FluentAssertions;
using Lld.Contracts;
using Lld.Domain.Progress;
using Lld.Wire;

namespace Lld.Progress.Tests;

public sealed class ProgressWireTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Snapshot_SurvivesTheRoundTrip()
    {
        var snapshot = new ProgressSnapshot(
            [
                new LessonProgress("srp", LessonStatus.Done, DoneBy.Manual, T0, ["rule"], new ReadingPosition("rule", 12), T0, T0, T0, "d"),
                new LessonProgress("ocp", LessonStatus.NotStarted, null, null, [], null, null, null, T0, "d"),
            ],
            [new DateOnly(2026, 10, 19)]);

        var back = ProgressWire.ToSnapshot(ProgressWire.ToDto(snapshot));

        back.Value.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public void Dto_UsesTheWireNames()
    {
        var dto = ProgressWire.ToDto(new LessonProgress("srp", LessonStatus.Done, DoneBy.Auto, T0, [], null, T0, null, T0, "d"));

        dto.Should().Match<LessonProgressDto>(d => d.Status == "done" && d.DoneBy == "auto");
    }

    [Theory]
    [InlineData("finished", null)]
    [InlineData("done", "magic")]
    public void UnknownNames_AreAValidationError(string status, string? doneBy)
    {
        var dto = new LessonProgressDto("srp", status, doneBy, T0, [], null, null, null, T0, "d");

        ProgressWire.ToLesson(dto).Error!.Code.Should().Be("validation.progress.status");
    }

    [Theory]
    [InlineData("2026-10-19", true)]
    [InlineData("19-10-2026", false)]
    [InlineData(null, false)]
    public void Days_MustBeIsoDates(string? text, bool valid)
    {
        ProgressWire.ToDay(text).IsSuccess.Should().Be(valid);
    }
}
