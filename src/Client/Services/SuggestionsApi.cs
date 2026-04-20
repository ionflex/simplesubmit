using System.Net.Http.Json;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Client.Services;

public sealed class SuggestionsApi(HttpClient http)
{
    private sealed record PendingCountResponse(int Count);

    public async Task<IReadOnlyList<SuggestionListItem>> ListAsync(CancellationToken ct = default)
    {
        var items = await http.GetFromJsonAsync<SuggestionListItem[]>("api/suggestions", ct);
        return items ?? [];
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<PendingCountResponse>("api/suggestions/pending-count", ct);
        return response?.Count ?? 0;
    }

    public async Task<SubmitSuggestionResponse> SubmitAsync(SubmitSuggestionRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/suggestions", request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SubmitSuggestionResponse>(ct);
        return body ?? throw new InvalidOperationException("Empty response from submit endpoint.");
    }

    public async Task<VoteResponse?> VoteAsync(Guid id, bool voted, CancellationToken ct = default)
    {
        var method = voted ? HttpMethod.Post : HttpMethod.Delete;
        using var request = new HttpRequestMessage(method, $"api/suggestions/{id}/vote");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VoteResponse>(ct);
    }
}
