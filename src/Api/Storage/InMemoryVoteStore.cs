using System.Collections.Concurrent;
using SimpleSubmit.Api.Identity;

namespace SimpleSubmit.Api.Storage;

public sealed class InMemoryVoteStore : IVoteStore
{
    private readonly ConcurrentDictionary<(Guid SuggestionId, string Voter), byte> _votes = new();

    public Task<bool> AddAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct)
    {
        return Task.FromResult(_votes.TryAdd((suggestionId, voter.Value), 0));
    }

    public Task<bool> RemoveAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct)
    {
        return Task.FromResult(_votes.TryRemove((suggestionId, voter.Value), out _));
    }

    public Task<int> CountAsync(Guid suggestionId, CancellationToken ct)
    {
        return Task.FromResult(_votes.Keys.Count(k => k.SuggestionId == suggestionId));
    }

    public Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct)
    {
        var set = suggestionIds.ToHashSet();
        var result = suggestionIds.ToDictionary(id => id, _ => 0);
        foreach (var key in _votes.Keys)
        {
            if (set.Contains(key.SuggestionId))
            {
                result[key.SuggestionId]++;
            }
        }
        return Task.FromResult<IReadOnlyDictionary<Guid, int>>(result);
    }

    public Task<IReadOnlySet<Guid>> GetVotedAsync(SubmitterId voter, IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct)
    {
        var set = suggestionIds.ToHashSet();
        var voted = _votes.Keys
            .Where(k => k.Voter == voter.Value && set.Contains(k.SuggestionId))
            .Select(k => k.SuggestionId)
            .ToHashSet();
        return Task.FromResult<IReadOnlySet<Guid>>(voted);
    }

    public Task<int> DeleteAllForSuggestionAsync(Guid suggestionId, CancellationToken ct)
    {
        var toRemove = _votes.Keys.Where(k => k.SuggestionId == suggestionId).ToList();
        var removed = 0;
        foreach (var key in toRemove)
        {
            if (_votes.TryRemove(key, out _))
            {
                removed++;
            }
        }
        return Task.FromResult(removed);
    }

    public Task<int> DeleteAllAsync(CancellationToken ct)
    {
        var count = _votes.Count;
        _votes.Clear();
        return Task.FromResult(count);
    }
}
