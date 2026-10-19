namespace Lld.Domain.Progress;

/// <summary>
/// The two ways two versions of a lesson's progress become one. Pure functions, so every store applies them the same way.
/// </summary>
public static class ProgressRules
{
    /// <summary>
    /// Normal writes by the same learner: the later edit wins (last-write-wins). On a tie the incoming record wins,
    /// so saving the same edit twice is harmless.
    /// </summary>
    public static LessonProgress Newer(LessonProgress stored, LessonProgress incoming)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.UpdatedAt >= stored.UpdatedAt ? incoming : stored;
    }

    /// <summary>
    /// Guest adoption (spec 005 §3.9): nothing read is lost. The furthest status wins, ticks are combined, the position
    /// comes from the more recently read side, and every date is the latest of the two.
    /// </summary>
    public static LessonProgress Merge(LessonProgress first, LessonProgress second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        if (first.LessonId != second.LessonId)
        {
            throw new ArgumentException(
                $"Only progress for the same lesson can be merged ('{first.LessonId}' vs '{second.LessonId}').", nameof(second));
        }

        var done = LaterCompletion(first, second);
        var (recent, older) = ReadAtOrMin(first) > ReadAtOrMin(second) ? (first, second) : (second, first);
        var lastEdit = first.UpdatedAt > second.UpdatedAt ? first : second;
        return new LessonProgress(
            first.LessonId,
            (LessonStatus)Math.Max((int)first.Status, (int)second.Status),
            done?.DoneBy,
            done?.CompletedAt,
            [.. first.ReadSections.Union(second.ReadSections, StringComparer.Ordinal)],
            recent.Position ?? older.Position,
            Latest(first.LastReadAt, second.LastReadAt),
            Latest(first.LastRevisedAt, second.LastRevisedAt),
            lastEdit.UpdatedAt,
            lastEdit.DeviceId);
    }

    /// <summary>Merges two learners' snapshots lesson by lesson; activity days are combined.</summary>
    public static ProgressSnapshot Merge(ProgressSnapshot first, ProgressSnapshot second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var lessons = first.Lessons.ToDictionary(lesson => lesson.LessonId, StringComparer.Ordinal);
        foreach (var lesson in second.Lessons)
        {
            lessons[lesson.LessonId] = lessons.TryGetValue(lesson.LessonId, out var existing) ? Merge(existing, lesson) : lesson;
        }

        return new ProgressSnapshot(
            [.. lessons.Values.OrderBy(lesson => lesson.LessonId, StringComparer.Ordinal)],
            [.. first.ActivityDays.Union(second.ActivityDays).Order()]);
    }

    /// <summary>The side whose completion counts: the done one, or the later finish when both are done.</summary>
    private static LessonProgress? LaterCompletion(LessonProgress first, LessonProgress second) =>
        (first.Status, second.Status) switch
        {
            (LessonStatus.Done, LessonStatus.Done) => first.CompletedAt > second.CompletedAt ? first : second,
            (LessonStatus.Done, _) => first,
            (_, LessonStatus.Done) => second,
            _ => null,
        };

    private static DateTimeOffset ReadAtOrMin(LessonProgress progress) => progress.LastReadAt ?? DateTimeOffset.MinValue;

    private static DateTimeOffset? Latest(DateTimeOffset? first, DateTimeOffset? second) =>
        first is null ? second : second is null ? first : first > second ? first : second;
}
