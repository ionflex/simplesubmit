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
    IVoteStore votes,
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
        var submitter = await identity.GetOrCreateAsync(req.HttpContext);
        var approved = await store.ListByStatusAsync(SuggestionStatus.Approved, ct);
        var ids = approved.Select(s => s.Id).ToArray();
        var counts = await votes.GetCountsAsync(ids, ct);
        var voted = await votes.GetVotedAsync(submitter, ids, ct);

        var items = approved
            .Select(s => new SuggestionListItem
            (
                Id: s.Id,
                Text: s.Text,
                AuthorName: s.AuthorName,
                SubmittedAtUtc: s.SubmittedAtUtc,
                VoteCount: counts.GetValueOrDefault(s.Id),
                HasVoted: voted.Contains(s.Id)
            ))
            .OrderByDescending(i => i.VoteCount)
            .ThenByDescending(i => i.SubmittedAtUtc)
            .ToArray();

        return Results.Ok(items);
    }

    [Function("PendingCount")]
    public async Task<IResult> PendingCountAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "suggestions/pending-count")] HttpRequest req,
        CancellationToken ct
    )
    {
        var pending = await store.ListByStatusAsync(SuggestionStatus.Pending, ct);
        return Results.Ok(new { count = pending.Count });
    }

    [Function("Vote")]
    public async Task<IResult> VoteAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "suggestions/{id:guid}/vote")] HttpRequest req,
        Guid id,
        CancellationToken ct
    )
    {
        return await ToggleVoteAsync(req, id, addVote: true, ct);
    }

    [Function("Unvote")]
    public async Task<IResult> UnvoteAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "suggestions/{id:guid}/vote")] HttpRequest req,
        Guid id,
        CancellationToken ct
    )
    {
        return await ToggleVoteAsync(req, id, addVote: false, ct);
    }

    private async Task<IResult> ToggleVoteAsync(HttpRequest req, Guid id, bool addVote, CancellationToken ct)
    {
        var suggestion = await store.GetAsync(id, ct);
        if (suggestion is null || suggestion.Status != SuggestionStatus.Approved)
        {
            return Results.NotFound();
        }

        var submitter = await identity.GetOrCreateAsync(req.HttpContext);

        if (addVote)
        {
            await votes.AddAsync(id, submitter, ct);
        }
        else
        {
            await votes.RemoveAsync(id, submitter, ct);
        }

        var count = await votes.CountAsync(id, ct);
        return Results.Ok(new VoteResponse(id, count, addVote));
    }
}
