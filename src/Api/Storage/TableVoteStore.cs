using Azure;
using Azure.Data.Tables;
using SimpleSubmit.Api.Identity;

namespace SimpleSubmit.Api.Storage;

public sealed class TableVoteStore : IVoteStore
{
    private readonly TableClient _table;

    public TableVoteStore(TableServiceClient service)
    {
        _table = service.GetTableClient("votes");
        _table.CreateIfNotExists();
    }

    public async Task<bool> AddAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct)
    {
        var entity = new TableEntity(PartitionKeyOf(suggestionId), voter.Value)
        {
            ["VotedAtUtc"] = DateTimeOffset.UtcNow,
        };
        try
        {
            await _table.AddEntityAsync(entity, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Guid suggestionId, SubmitterId voter, CancellationToken ct)
    {
        try
        {
            await _table.DeleteEntityAsync(PartitionKeyOf(suggestionId), voter.Value, ETag.All, ct);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<int> CountAsync(Guid suggestionId, CancellationToken ct)
    {
        var filter = $"PartitionKey eq '{PartitionKeyOf(suggestionId)}'";
        var count = 0;
        await foreach (var _ in _table.QueryAsync<TableEntity>(filter: filter, select: ["RowKey"], cancellationToken: ct))
        {
            count++;
        }
        return count;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsAsync(IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct)
    {
        var counts = suggestionIds.ToDictionary(id => id, _ => 0);
        if (counts.Count == 0)
        {
            return counts;
        }

        var filter = string.Join
        (
            " or ",
            suggestionIds.Select(id => $"PartitionKey eq '{PartitionKeyOf(id)}'")
        );

        await foreach (var entity in _table.QueryAsync<TableEntity>(filter: filter, select: ["PartitionKey"], cancellationToken: ct))
        {
            if (Guid.TryParseExact(entity.PartitionKey, "N", out var id) && counts.ContainsKey(id))
            {
                counts[id]++;
            }
        }
        return counts;
    }

    public async Task<IReadOnlySet<Guid>> GetVotedAsync(SubmitterId voter, IReadOnlyCollection<Guid> suggestionIds, CancellationToken ct)
    {
        var voted = new HashSet<Guid>();
        if (suggestionIds.Count == 0)
        {
            return voted;
        }

        var partitionsOr = string.Join
        (
            " or ",
            suggestionIds.Select(id => $"PartitionKey eq '{PartitionKeyOf(id)}'")
        );
        var filter = $"({partitionsOr}) and RowKey eq '{voter.Value}'";

        await foreach (var entity in _table.QueryAsync<TableEntity>(filter: filter, select: ["PartitionKey"], cancellationToken: ct))
        {
            if (Guid.TryParseExact(entity.PartitionKey, "N", out var id))
            {
                voted.Add(id);
            }
        }
        return voted;
    }

    private static string PartitionKeyOf(Guid suggestionId) => suggestionId.ToString("N");
}
