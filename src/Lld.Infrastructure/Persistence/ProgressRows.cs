using System.Text.Json;
using Lld.Domain.Progress;

namespace Lld.Infrastructure.Persistence;

/// <summary>
/// A <see cref="LessonProgress"/> as the API stores it: one row per user and lesson. Mutable and flat because EF Core
/// maps it; the domain record stays persistence-free.
/// </summary>
public sealed class LessonProgressRow
{
    public string OwnerId { get; set; } = string.Empty;

    public string LessonId { get; set; } = string.Empty;

    public LessonStatus Status { get; set; }

    public DoneBy? DoneBy { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string ReadSectionsJson { get; set; } = "[]";

    public string? PositionAnchor { get; set; }

    public int? PositionOffset { get; set; }

    public DateTimeOffset? LastReadAt { get; set; }

    public DateTimeOffset? LastRevisedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public static LessonProgressRow From(string ownerId, LessonProgress lesson) =>
        new LessonProgressRow { OwnerId = ownerId, LessonId = lesson.LessonId }.CopyFrom(lesson);

    public LessonProgressRow CopyFrom(LessonProgress lesson)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        Status = lesson.Status;
        DoneBy = lesson.DoneBy;
        CompletedAt = lesson.CompletedAt;
        ReadSectionsJson = JsonSerializer.Serialize(lesson.ReadSections);
        PositionAnchor = lesson.Position?.Anchor;
        PositionOffset = lesson.Position?.Offset;
        LastReadAt = lesson.LastReadAt;
        LastRevisedAt = lesson.LastRevisedAt;
        UpdatedAt = lesson.UpdatedAt;
        DeviceId = lesson.DeviceId;
        return this;
    }

    public LessonProgress ToLesson() =>
        new(
            LessonId,
            Status,
            DoneBy,
            CompletedAt,
            JsonSerializer.Deserialize<string[]>(ReadSectionsJson) ?? [],
            PositionAnchor is null ? null : new ReadingPosition(PositionAnchor, PositionOffset ?? 0),
            LastReadAt,
            LastRevisedAt,
            UpdatedAt,
            DeviceId);
}

/// <summary>A local day on which a user studied (spec 005 §3.6).</summary>
public sealed class ActivityDayRow
{
    public string OwnerId { get; set; } = string.Empty;

    public DateOnly Day { get; set; }
}
