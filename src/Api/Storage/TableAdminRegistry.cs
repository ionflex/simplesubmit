using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public sealed class TableAdminRegistry : IAdminRegistry
{
    private const string PartitionKey = "admin";
    private readonly TableClient _table;
    private readonly string? _bootstrapPrincipalId;

    public TableAdminRegistry(TableServiceClient service, IConfiguration config)
    {
        _table = service.GetTableClient("admins");
        _table.CreateIfNotExists();
        _bootstrapPrincipalId = config["ADMIN_PRINCIPAL_ID"];
    }

    public async Task<bool> IsActiveAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            var response = await _table.GetEntityAsync<AdminEntity>(PartitionKey, principalId, cancellationToken: ct);
            return response.Value.Status == (int)AdminStatus.Active;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<AdminEntry?> GetAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return BootstrapEntry(principalId);
        }

        try
        {
            var response = await _table.GetEntityAsync<AdminEntity>(PartitionKey, principalId, cancellationToken: ct);
            return ToDomain(response.Value, isBootstrap: false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<AdminEntry>> ListAsync(CancellationToken ct)
    {
        var entries = new List<AdminEntry>();
        if (!string.IsNullOrEmpty(_bootstrapPrincipalId))
        {
            entries.Add(BootstrapEntry(_bootstrapPrincipalId));
        }

        await foreach (var entity in _table.QueryAsync<AdminEntity>(filter: $"PartitionKey eq '{PartitionKey}'", cancellationToken: ct))
        {
            if (string.Equals(entity.RowKey, _bootstrapPrincipalId, StringComparison.Ordinal))
            {
                continue;
            }
            entries.Add(ToDomain(entity, isBootstrap: false));
        }

        return entries
            .OrderByDescending(e => e.IsBootstrap)
            .ThenBy(e => e.Status)
            .ThenByDescending(e => e.RequestedAtUtc)
            .ToArray();
    }

    public async Task<AdminEntry> UpsertPendingAsync(string principalId, string displayName, string identityProvider, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return BootstrapEntry(principalId);
        }

        var existing = await GetAsync(principalId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var entity = new AdminEntity
        {
            PartitionKey = PartitionKey,
            RowKey = principalId,
            DisplayName = displayName,
            IdentityProvider = identityProvider,
            Status = (int)AdminStatus.Pending,
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            await _table.AddEntityAsync(entity, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Race: someone inserted at the same time — re-read.
            var response = await _table.GetEntityAsync<AdminEntity>(PartitionKey, principalId, cancellationToken: ct);
            entity = response.Value;
        }
        return ToDomain(entity, isBootstrap: false);
    }

    public async Task<AdminEntry?> ApproveAsync(string principalId, string approvedByPrincipalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return BootstrapEntry(principalId);
        }

        AdminEntity entity;
        try
        {
            var response = await _table.GetEntityAsync<AdminEntity>(PartitionKey, principalId, cancellationToken: ct);
            entity = response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        entity.Status = (int)AdminStatus.Active;
        entity.ActivatedAtUtc = DateTimeOffset.UtcNow;
        entity.ActivatedByPrincipalId = approvedByPrincipalId;
        await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, ct);
        return ToDomain(entity, isBootstrap: false);
    }

    public async Task<bool> RemoveAsync(string principalId, CancellationToken ct)
    {
        if (string.Equals(principalId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            await _table.DeleteEntityAsync(PartitionKey, principalId, ETag.All, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    private AdminEntry BootstrapEntry(string principalId) => new
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

    private static AdminEntry ToDomain(AdminEntity e, bool isBootstrap) => new
    (
        PrincipalId: e.RowKey,
        DisplayName: e.DisplayName ?? "",
        IdentityProvider: e.IdentityProvider ?? "",
        Status: (AdminStatus)e.Status,
        RequestedAtUtc: e.RequestedAtUtc,
        ActivatedAtUtc: e.ActivatedAtUtc,
        ActivatedByPrincipalId: e.ActivatedByPrincipalId,
        IsBootstrap: isBootstrap
    );

    private sealed class AdminEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? DisplayName { get; set; }
        public string? IdentityProvider { get; set; }
        public int Status { get; set; }
        public DateTimeOffset RequestedAtUtc { get; set; }
        public DateTimeOffset? ActivatedAtUtc { get; set; }
        public string? ActivatedByPrincipalId { get; set; }
    }
}
