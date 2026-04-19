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
}
