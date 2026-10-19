using System.Globalization;
using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Domain.Progress;

namespace Lld.Wire;

/// <summary>
/// Progress records ↔ <see cref="LessonProgressDto"/>. Reading from the wire can fail (an unknown status, a bad date),
/// so those directions return a <see cref="Result"/> instead of throwing.
/// </summary>
public static class ProgressWire
{
    public const string DayFormat = "yyyy-MM-dd";

    private static readonly Error UnknownStatus = Error.Validation("progress.status", "Unknown lesson status.");
    private static readonly Error BadDay = Error.Validation("progress.day", $"A study day must be a date like {DayFormat}.");

    public static ProgressSnapshotDto ToDto(ProgressSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new ProgressSnapshotDto([.. snapshot.Lessons.Select(ToDto)], [.. snapshot.ActivityDays.Select(ToText)]);
    }

    public static LessonProgressDto ToDto(LessonProgress lesson)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        return new LessonProgressDto(
            lesson.LessonId,
            ToText(lesson.Status),
            lesson.DoneBy is { } doneBy ? ToText(doneBy) : null,
            lesson.CompletedAt,
            lesson.ReadSections,
            lesson.Position is { } position ? new ReadingPositionDto(position.Anchor, position.Offset) : null,
            lesson.LastReadAt,
            lesson.LastRevisedAt,
            lesson.UpdatedAt,
            lesson.DeviceId);
    }

    public static Result<ProgressSnapshot> ToSnapshot(ProgressSnapshotDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var lessons = ToLessons(dto.Lessons ?? []);
        if (lessons.IsFailure)
        {
            return lessons.Error!;
        }

        var days = new List<DateOnly>();
        foreach (var text in dto.ActivityDays ?? [])
        {
            var day = ToDay(text);
            if (day.IsFailure)
            {
                return day.Error!;
            }

            days.Add(day.Value);
        }

        return new ProgressSnapshot(lessons.Value, days);
    }

    public static Result<IReadOnlyList<LessonProgress>> ToLessons(IEnumerable<LessonProgressDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        var lessons = new List<LessonProgress>();
        foreach (var dto in dtos)
        {
            var lesson = ToLesson(dto);
            if (lesson.IsFailure)
            {
                return lesson.Error!;
            }

            lessons.Add(lesson.Value);
        }

        return lessons;
    }

    public static Result<LessonProgress> ToLesson(LessonProgressDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!TryParseStatus(dto.Status, out var status) || !TryParseDoneBy(dto.DoneBy, out var doneBy))
        {
            return UnknownStatus;
        }

        return new LessonProgress(
            dto.LessonId ?? string.Empty,
            status,
            doneBy,
            dto.CompletedAt,
            dto.ReadSections ?? [],
            dto.Position is { } position ? new ReadingPosition(position.Anchor ?? string.Empty, position.Offset) : null,
            dto.LastReadAt,
            dto.LastRevisedAt,
            dto.UpdatedAt,
            dto.DeviceId ?? string.Empty);
    }

    public static Result<DateOnly> ToDay(string? text) =>
        DateOnly.TryParseExact(text, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : BadDay;

    public static string ToText(DateOnly day) => day.ToString(DayFormat, CultureInfo.InvariantCulture);

    private static bool TryParseStatus(string? text, out LessonStatus status)
    {
        (var known, status) = text switch
        {
            ProgressStatuses.NotStarted => (true, LessonStatus.NotStarted),
            ProgressStatuses.InProgress => (true, LessonStatus.InProgress),
            ProgressStatuses.Done => (true, LessonStatus.Done),
            _ => (false, default),
        };
        return known;
    }

    private static bool TryParseDoneBy(string? text, out DoneBy? doneBy)
    {
        (var known, doneBy) = text switch
        {
            null => (true, (DoneBy?)null),
            ProgressDoneBy.Auto => (true, DoneBy.Auto),
            ProgressDoneBy.Manual => (true, DoneBy.Manual),
            _ => (false, null),
        };
        return known;
    }

    private static string ToText(LessonStatus status) => status switch
    {
        LessonStatus.NotStarted => ProgressStatuses.NotStarted,
        LessonStatus.InProgress => ProgressStatuses.InProgress,
        LessonStatus.Done => ProgressStatuses.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown lesson status."),
    };

    private static string ToText(DoneBy doneBy) => doneBy == DoneBy.Manual ? ProgressDoneBy.Manual : ProgressDoneBy.Auto;
}
