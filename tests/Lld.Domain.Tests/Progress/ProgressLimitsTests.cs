using FluentAssertions;
using Lld.Domain.Progress;
using static Lld.Domain.Tests.Progress.ProgressBuilder;

namespace Lld.Domain.Tests.Progress;

public sealed class ProgressLimitsTests
{
    public static TheoryData<LessonProgress, string> InvalidRecords => new()
    {
        { Opened("") , "validation.progress.lesson_id" },
        { Opened(new string('x', ProgressLimits.MaxLessonIdLength + 1)), "validation.progress.lesson_id" },
        { Opened() with { ReadSections = [.. Enumerable.Range(0, ProgressLimits.MaxSections + 1).Select(i => $"s{i}")] }, "validation.progress.sections" },
        { Opened() with { ReadSections = [""] }, "validation.progress.anchor" },
        { Opened() with { Position = new ReadingPosition(new string('a', ProgressLimits.MaxAnchorLength + 1), 0) }, "validation.progress.anchor" },
        { Opened() with { Position = new ReadingPosition("rule", -1) }, "validation.progress.position" },
        { Opened() with { DeviceId = new string('d', ProgressLimits.MaxDeviceIdLength + 1) }, "validation.progress.device_id" },
        { Opened() with { Status = (LessonStatus)7 }, "validation.progress.status" },
        { Opened() with { Status = LessonStatus.Done }, "validation.progress.status" },
        { Done() with { CompletedAt = null }, "validation.progress.status" },
        { Opened() with { DoneBy = DoneBy.Manual }, "validation.progress.status" },
    };

    [Theory]
    [MemberData(nameof(InvalidRecords))]
    public void Validate_RejectsARecordOutsideTheLimits(LessonProgress record, string expectedCode)
    {
        ProgressLimits.Validate(record).Error!.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Validate_AcceptsOpenedDoneAndResetRecords()
    {
        var reset = Opened() with { Status = LessonStatus.NotStarted, LastReadAt = null, Position = new ReadingPosition("", 0) };

        ProgressLimits.Validate([Opened(), Done(), reset]).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsTooManyLessonsInOneWrite()
    {
        var lessons = Enumerable.Range(0, ProgressLimits.MaxLessonsPerWrite + 1).Select(i => Opened($"l{i}")).ToList();

        ProgressLimits.Validate(lessons).Error!.Code.Should().Be("validation.progress.too_many");
    }
}
