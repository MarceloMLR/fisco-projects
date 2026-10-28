using System.Collections.Concurrent;
using FiscoProjects.Users.Application;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The photos read from Microsoft 365, kept only in the memory of the application, never in the database (ADR-0124):
/// the photo, or that there is none, for an hour, and a failure of Graph for a minute. One entry per person of the
/// company, of a few KB; the expired ones leave at the next one kept.
/// </summary>
internal sealed class PersonPhotoCache(TimeProvider time)
{
    public static readonly TimeSpan Kept = TimeSpan.FromHours(1);

    public static readonly TimeSpan KeptAfterFailure = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>Whether the answer for the key is kept and still valid, with the photo, or none.</summary>
    public bool TryGet(string key, out PersonPhoto? photo)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.Until > time.GetUtcNow())
        {
            photo = entry.Photo;
            return true;
        }

        photo = null;
        return false;
    }

    public void Keep(string key, PersonPhoto? photo, TimeSpan duration)
    {
        var now = time.GetUtcNow();
        foreach (var expired in _entries.Where(pair => pair.Value.Until <= now))
        {
            _entries.TryRemove(expired);
        }

        _entries[key] = new Entry(photo, now + duration);
    }

    private sealed record Entry(PersonPhoto? Photo, DateTimeOffset Until);
}
