using System.Collections.Concurrent;
using SimpleSubmit.Api.Identity;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public sealed class InMemorySuggestionStore : ISuggestionStore
{
    private readonly ConcurrentDictionary<Guid, Suggestion> _items = new();

    public Task<Suggestion> AddAsync(string text, string? authorName, SubmitterId submitter, CancellationToken ct)
    {
        var item = new Suggestion
        (
            Id: Guid.NewGuid(),
            Text: text,
            AuthorName: authorName,
            SubmittedAtUtc: DateTimeOffset.UtcNow,
            Status: SuggestionStatus.Pending,
            ModeratedAtUtc: null
        );
        _items[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<Suggestion?> GetAsync(Guid id, CancellationToken ct)
    {
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<Suggestion>> ListByStatusAsync(SuggestionStatus status, CancellationToken ct)
    {
        IReadOnlyList<Suggestion> snapshot = _items.Values
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToArray();
        return Task.FromResult(snapshot);
    }

    public Task<Suggestion?> SetStatusAsync(Guid id, SuggestionStatus newStatus, CancellationToken ct)
    {
        if (!_items.TryGetValue(id, out var existing))
        {
            return Task.FromResult<Suggestion?>(null);
        }

        var updated = existing with
        {
            Status = newStatus,
            ModeratedAtUtc = DateTimeOffset.UtcNow,
        };
        _items[id] = updated;
        return Task.FromResult<Suggestion?>(updated);
    }

    public Task<int> PurgeRejectedOlderThanAsync(TimeSpan age, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - age;
        var toRemove = _items.Values
            .Where(s => s.Status == SuggestionStatus.Rejected && (s.ModeratedAtUtc ?? s.SubmittedAtUtc) < cutoff)
            .Select(s => s.Id)
            .ToList();

        var removed = 0;
        foreach (var id in toRemove)
        {
            if (_items.TryRemove(id, out _))
            {
                removed++;
            }
        }
        return Task.FromResult(removed);
    }
}
