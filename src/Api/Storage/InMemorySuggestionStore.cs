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
            SubmittedAtUtc: DateTimeOffset.UtcNow
        );
        _items[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<Suggestion>> ListAsync(CancellationToken ct)
    {
        IReadOnlyList<Suggestion> snapshot = _items.Values
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToArray();
        return Task.FromResult(snapshot);
    }
}
