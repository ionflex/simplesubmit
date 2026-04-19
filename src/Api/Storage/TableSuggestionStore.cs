using Azure;
using Azure.Data.Tables;
using SimpleSubmit.Api.Identity;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Storage;

public sealed class TableSuggestionStore : ISuggestionStore
{
    private const string PartitionKey = "SUG";
    private readonly TableClient _table;

    public TableSuggestionStore(TableServiceClient service)
    {
        _table = service.GetTableClient("suggestions");
        _table.CreateIfNotExists();
    }

    public async Task<Suggestion> AddAsync(string text, string? authorName, SubmitterId submitter, CancellationToken ct)
    {
        var entity = new SuggestionEntity
        {
            PartitionKey = PartitionKey,
            RowKey = Guid.NewGuid().ToString("N"),
            Text = text,
            AuthorName = authorName,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Status = (int)SuggestionStatus.Pending,
            SubmitterId = submitter.Value,
        };
        await _table.AddEntityAsync(entity, ct);
        return ToDomain(entity);
    }

    public async Task<Suggestion?> GetAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var response = await _table.GetEntityAsync<SuggestionEntity>(PartitionKey, id.ToString("N"), cancellationToken: ct);
            return ToDomain(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Suggestion>> ListByStatusAsync(SuggestionStatus status, CancellationToken ct)
    {
        var filter = $"PartitionKey eq '{PartitionKey}' and Status eq {(int)status}";
        var results = new List<Suggestion>();
        await foreach (var entity in _table.QueryAsync<SuggestionEntity>(filter: filter, cancellationToken: ct))
        {
            results.Add(ToDomain(entity));
        }
        results.Sort((a, b) => b.SubmittedAtUtc.CompareTo(a.SubmittedAtUtc));
        return results;
    }

    public async Task<Suggestion?> SetStatusAsync(Guid id, SuggestionStatus newStatus, CancellationToken ct)
    {
        SuggestionEntity entity;
        try
        {
            var response = await _table.GetEntityAsync<SuggestionEntity>(PartitionKey, id.ToString("N"), cancellationToken: ct);
            entity = response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        entity.Status = (int)newStatus;
        entity.ModeratedAtUtc = DateTimeOffset.UtcNow;
        await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, ct);
        return ToDomain(entity);
    }

    public async Task<int> PurgeRejectedOlderThanAsync(TimeSpan age, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - age;
        var filter = $"PartitionKey eq '{PartitionKey}' and Status eq {(int)SuggestionStatus.Rejected}";
        var removed = 0;
        await foreach (var entity in _table.QueryAsync<SuggestionEntity>(filter: filter, cancellationToken: ct))
        {
            var timestamp = entity.ModeratedAtUtc ?? entity.SubmittedAtUtc;
            if (timestamp >= cutoff) continue;

            try
            {
                await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag, ct);
                removed++;
            }
            catch (RequestFailedException ex) when (ex.Status == 404 || ex.Status == 412)
            {
                // Already gone, or changed under us — skip.
            }
        }
        return removed;
    }

    private static Suggestion ToDomain(SuggestionEntity e) => new
    (
        Id: Guid.ParseExact(e.RowKey, "N"),
        Text: e.Text ?? "",
        AuthorName: e.AuthorName,
        SubmittedAtUtc: e.SubmittedAtUtc,
        Status: (SuggestionStatus)e.Status,
        ModeratedAtUtc: e.ModeratedAtUtc
    );

    private sealed class SuggestionEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? Text { get; set; }
        public string? AuthorName { get; set; }
        public DateTimeOffset SubmittedAtUtc { get; set; }
        public int Status { get; set; }
        public DateTimeOffset? ModeratedAtUtc { get; set; }
        public string? SubmitterId { get; set; }
    }
}
