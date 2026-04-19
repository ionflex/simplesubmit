using System.Net;
using System.Net.Http.Json;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Client.Services;

public sealed class AdminApi(HttpClient http)
{
    public enum Outcome { Ok, Unauthorized, Forbidden }

    public async Task<(Outcome outcome, IReadOnlyList<Suggestion> items)> ListPendingAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/mod/suggestions/pending", ct);
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
        return await PostNoBodyJsonAsync<Suggestion>($"api/mod/suggestions/{id}/approve", ct);
    }

    public async Task<Suggestion?> RejectAsync(Guid id, CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<Suggestion>($"api/mod/suggestions/{id}/reject", ct);
    }

    public async Task<int> PurgeRejectedAsync(int olderThanDays, CancellationToken ct = default)
    {
        var body = await PostNoBodyJsonAsync<PurgeRejectedResponse>($"api/mod/purge-rejected?olderThanDays={olderThanDays}", ct);
        return body?.PurgedCount ?? 0;
    }

    private async Task<T?> PostNoBodyJsonAsync<T>(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
}
