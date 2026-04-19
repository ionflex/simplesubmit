using System.Net;
using System.Net.Http.Json;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Client.Services;

public sealed class AdminApi(HttpClient http)
{
    public enum Outcome { Ok, Unauthorized, Forbidden }

    public async Task<(Outcome outcome, IReadOnlyList<Suggestion> items)> ListPendingAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/admin/suggestions/pending", ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (Outcome.Unauthorized, []);
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return (Outcome.Forbidden, []);
        }
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<Suggestion[]>(ct) ?? [];
        return (Outcome.Ok, items);
    }

    public async Task<Suggestion?> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/admin/suggestions/{id}/approve", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Suggestion>(ct);
    }

    public async Task<Suggestion?> RejectAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/admin/suggestions/{id}/reject", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Suggestion>(ct);
    }

    public async Task<int> PurgeRejectedAsync(int olderThanDays, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/admin/purge-rejected?olderThanDays={olderThanDays}", content: null, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PurgeRejectedResponse>(ct);
        return body?.PurgedCount ?? 0;
    }
}
