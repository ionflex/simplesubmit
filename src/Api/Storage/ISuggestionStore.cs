using SimpleSubmit.Api.Identity;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public interface ISuggestionStore
{
    Task<Suggestion> AddAsync(string text, string? authorName, SubmitterId submitter, CancellationToken ct);

    Task<Suggestion?> GetAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> ListByStatusAsync(SuggestionStatus status, CancellationToken ct);

    Task<Suggestion?> SetStatusAsync(Guid id, SuggestionStatus newStatus, CancellationToken ct);

    Task<int> PurgeRejectedOlderThanAsync(TimeSpan age, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> ListAllAsync(CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    Task<int> DeleteAllAsync(CancellationToken ct);
}
