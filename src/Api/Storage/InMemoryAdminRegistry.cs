using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public sealed class InMemoryAdminRegistry(IConfiguration config) : IAdminRegistry
{
    private readonly ConcurrentDictionary<string, AdminEntry> _entries = new();
    private readonly string? _bootstrapPrincipalId = config["ADMIN_PRINCIPAL_ID"];

    public Task<bool> IsActiveAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(_entries.TryGetValue(principalId, out var e) && e.Status == AdminStatus.Active);
    }

    public Task<AdminEntry?> GetAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Task.FromResult<AdminEntry?>(Bootstrap(principalId));
        }
        _entries.TryGetValue(principalId, out var entry);
        return Task.FromResult(entry);
    }

    public Task<IReadOnlyList<AdminEntry>> ListAsync(CancellationToken ct)
    {
        var list = new List<AdminEntry>();
        if (!string.IsNullOrEmpty(_bootstrapPrincipalId))
        {
            list.Add(Bootstrap(_bootstrapPrincipalId));
        }
        list.AddRange(_entries.Values.Where(e => !e.IsBootstrap));
        IReadOnlyList<AdminEntry> ordered = list
            .OrderByDescending(e => e.IsBootstrap)
            .ThenBy(e => e.Status)
            .ThenByDescending(e => e.RequestedAtUtc)
            .ToArray();
        return Task.FromResult(ordered);
    }

    public Task<AdminEntry> UpsertPendingAsync(string principalId, string displayName, string identityProvider, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Task.FromResult(Bootstrap(principalId));
        }

        var entry = _entries.GetOrAdd(principalId, _ => new AdminEntry
        (
            PrincipalId: principalId,
            DisplayName: displayName,
            IdentityProvider: identityProvider,
            Status: AdminStatus.Pending,
            RequestedAtUtc: DateTimeOffset.UtcNow,
            ActivatedAtUtc: null,
            ActivatedByPrincipalId: null,
            IsBootstrap: false
        ));
        return Task.FromResult(entry);
    }

    public Task<AdminEntry?> ApproveAsync(string principalId, string approvedByPrincipalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Task.FromResult<AdminEntry?>(Bootstrap(principalId));
        }

        if (!_entries.TryGetValue(principalId, out var existing))
        {
            return Task.FromResult<AdminEntry?>(null);
        }

        var updated = existing with
        {
            Status = AdminStatus.Active,
            ActivatedAtUtc = DateTimeOffset.UtcNow,
            ActivatedByPrincipalId = approvedByPrincipalId,
        };
        _entries[principalId] = updated;
        return Task.FromResult<AdminEntry?>(updated);
    }

    public Task<bool> RemoveAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(_entries.TryRemove(principalId, out _));
    }

    private static AdminEntry Bootstrap(string principalId) => new
    (
        PrincipalId: principalId,
        DisplayName: "bootstrap",
        IdentityProvider: "config",
        Status: AdminStatus.Active,
        RequestedAtUtc: DateTimeOffset.MinValue,
        ActivatedAtUtc: DateTimeOffset.MinValue,
        ActivatedByPrincipalId: null,
        IsBootstrap: true
    );
}
