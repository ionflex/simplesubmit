using SimpleSubmit.Api.Identity;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public interface ISuggestionStore
{
    Task<Suggestion> AddAsync(string text, string? authorName, SubmitterId submitter, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> ListAsync(CancellationToken ct);
}
