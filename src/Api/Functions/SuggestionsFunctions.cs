using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SimpleSubmit.Api.Identity;
using SimpleSubmit.Api.Storage;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Functions;

public sealed class SuggestionsFunctions
(
    ISubmitterIdentity identity,
    ISuggestionStore store,
    ILogger<SuggestionsFunctions> logger
)
{
    private const int MaxTextLength = 500;
    private const int MaxAuthorLength = 60;

    [Function("SubmitSuggestion")]
    public async Task<IResult> SubmitAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "suggestions")] HttpRequest req,
        CancellationToken ct
    )
    {
        var body = await req.ReadFromJsonAsync<SubmitSuggestionRequest>(ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Text))
        {
            return Results.BadRequest(new { error = "text required" });
        }

        var text = body.Text.Trim();
        if (text.Length > MaxTextLength)
        {
            return Results.BadRequest(new { error = $"text exceeds {MaxTextLength} characters" });
        }

        var author = body.AuthorName?.Trim();
        if (author is not null && author.Length > MaxAuthorLength)
        {
            return Results.BadRequest(new { error = $"authorName exceeds {MaxAuthorLength} characters" });
        }
        if (string.IsNullOrWhiteSpace(author))
        {
            author = null;
        }

        var submitter = await identity.GetOrCreateAsync(req.HttpContext);
        var saved = await store.AddAsync(text, author, submitter, ct);

        logger.LogInformation("Suggestion {Id} submitted by {Submitter}", saved.Id, submitter);

        return Results.Ok(new SubmitSuggestionResponse(saved.Id));
    }

    [Function("ListSuggestions")]
    public async Task<IResult> ListAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "suggestions")] HttpRequest req,
        CancellationToken ct
    )
    {
        _ = await identity.GetOrCreateAsync(req.HttpContext);
        var items = await store.ListByStatusAsync(SuggestionStatus.Approved, ct);
        return Results.Ok(items);
    }
}
