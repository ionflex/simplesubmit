using SimpleSubmit.Api.Identity;

namespace SimpleSubmit.Api.Storage;

public interface IVoteStore
{
    /// <summary>Records a vote. Returns true if inserted, false if the voter had already voted.</summary>
    Task<bool> AddAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct);

    /// <summary>Removes a vote. Returns true if deleted, false if no vote existed.</summary>
    Task<bool> RemoveAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct);

    /// <summary>Returns the current vote count for a single suggestion.</summary>
    Task<int> CountAsync(Guid suggestionId, CancellationToken ct);

    /// <summary>Returns vote counts for all suggestions in the given set (zero-entries included).</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct);

    /// <summary>Returns the subset of suggestion ids that the given voter has voted on.</summary>
    Task<IReadOnlySet<Guid>> GetVotedAsync(SubmitterId voter, IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct);
}
