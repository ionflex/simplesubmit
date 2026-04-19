using System.Net.Http.Json;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Client.Services;

public sealed class SuggestionsApi(HttpClient http)
{
    public async Task<IReadOnlyList<Suggestion>> ListAsync(CancellationToken ct = default)
    {
        var items = await http.GetFromJsonAsync<Suggestion[]>("api/suggestions", ct);
        return items ?? [];
    }

    public async Task<SubmitSuggestionResponse> SubmitAsync(SubmitSuggestionRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/suggestions", request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SubmitSuggestionResponse>(ct);
        return body ?? throw new InvalidOperationException("Empty response from submit endpoint.");
    }
}
