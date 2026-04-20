using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public interface IAdminRegistry
{
    Task<bool> IsActiveAsync(string principalId, CancellationToken ct);

    Task<AdminEntry?> GetAsync(string principalId, CancellationToken ct);

    Task<IReadOnlyList<AdminEntry>> ListAsync(CancellationToken ct);

    /// <summary>Creates a Pending entry if none exists for the principal. Returns the entry (new or existing).</summary>
    Task<AdminEntry> UpsertPendingAsync(string principalId, string displayName, string identityProvider, CancellationToken ct);

    /// <summary>Promotes a Pending entry to Active. Returns the updated entry, or null if no entry exists.</summary>
    Task<AdminEntry?> ApproveAsync(string principalId, string approvedByPrincipalId, CancellationToken ct);

    /// <summary>Removes an entry (rejects pending or revokes active). Returns true if something was removed.</summary>
    Task<bool> RemoveAsync(string principalId, CancellationToken ct);
}
