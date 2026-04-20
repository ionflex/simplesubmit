using System.Net;
using System.Net.Http.Json;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Client.Services;

public sealed class AdminApi(HttpClient http)
{
    public enum Outcome { Ok, Unauthorized, Forbidden }

    public async Task<AdminSelfStatus> GetSelfAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/mod/self", ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new AdminSelfStatus(false, false, null, null, null, null);
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdminSelfStatus>(ct)
            ?? new AdminSelfStatus(false, false, null, null, null, null);
    }

    public async Task<AdminEntry?> RequestAccessAsync(CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<AdminEntry>("api/mod/admin-requests", ct);
    }

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

    public async Task<Suggestion?> ApproveSuggestionAsync(Guid id, CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<Suggestion>($"api/mod/suggestions/{id}/approve", ct);
    }

    public async Task<Suggestion?> RejectSuggestionAsync(Guid id, CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<Suggestion>($"api/mod/suggestions/{id}/reject", ct);
    }

    public async Task<IReadOnlyList<Suggestion>> ListAllSuggestionsAsync(CancellationToken ct = default)
    {
        var items = await http.GetFromJsonAsync<Suggestion[]>("api/mod/all-suggestions", ct);
        return items ?? [];
    }

    public async Task DeleteSuggestionAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/mod/suggestions/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PurgeAllResponse?> PurgeAllAsync(CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<PurgeAllResponse>("api/mod/purge-all", ct);
    }

    public async Task<int> PurgeRejectedAsync(int olderThanDays, CancellationToken ct = default)
    {
        var body = await PostNoBodyJsonAsync<PurgeRejectedResponse>($"api/mod/purge-rejected?olderThanDays={olderThanDays}", ct);
        return body?.PurgedCount ?? 0;
    }

    public async Task<IReadOnlyList<AdminEntry>> ListAdminsAsync(CancellationToken ct = default)
    {
        var items = await http.GetFromJsonAsync<AdminEntry[]>("api/mod/admins", ct);
        return items ?? [];
    }

    public async Task<AdminEntry?> ApproveAdminAsync(string principalId, CancellationToken ct = default)
    {
        return await PostNoBodyJsonAsync<AdminEntry>($"api/mod/admins/{Uri.EscapeDataString(principalId)}/approve", ct);
    }

    public async Task RemoveAdminAsync(string principalId, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/mod/admins/{Uri.EscapeDataString(principalId)}", ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T?> PostNoBodyJsonAsync<T>(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
}
