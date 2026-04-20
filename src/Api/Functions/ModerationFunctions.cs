using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SimpleSubmit.Api.Identity;
using SimpleSubmit.Api.Storage;
using SimpleSubmit.Shared.Contracts;

namespace SimpleSubmit.Api.Functions;

public sealed class ModerationFunctions
(
    ISuggestionStore store,
    IAdminAuthorization admin,
    ILogger<ModerationFunctions> logger
)
{
    private const int DefaultPurgeAgeDays = 30;

    [Function("DiagWhoAmI")]
    public IResult DiagWhoAmI
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_diag/whoami")] HttpRequest req
    )
    {
        var isAdmin = admin.IsAdmin(req.HttpContext);
        var principalHeader = req.Headers.TryGetValue("x-ms-client-principal", out var values)
            ? values.ToString()
            : null;
        return Results.Ok(new
        {
            IsAdmin = isAdmin,
            HasPrincipalHeader = !string.IsNullOrEmpty(principalHeader),
            AdminPrincipalIdConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ADMIN_PRINCIPAL_ID")),
        });
    }

    [Function("DiagPendingCount")]
    public async Task<IResult> DiagPendingCount
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_diag/pending-count")] HttpRequest req,
        CancellationToken ct
    )
    {
        var items = await store.ListByStatusAsync(SuggestionStatus.Pending, ct);
        return Results.Ok(new { count = items.Count });
    }

    [Function("DiagPurgeAccess")]
    public IResult DiagPurgeAccess
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "_diag/purge-access")] HttpRequest req
    )
    {
        return Results.Ok(new { reachable = true });
    }

    [Function("ListPending")]
    public async Task<IResult> ListPendingAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mod/suggestions/pending")] HttpRequest req,
        CancellationToken ct
    )
    {
        if (!admin.IsAdmin(req.HttpContext))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var items = await store.ListByStatusAsync(SuggestionStatus.Pending, ct);
        return Results.Ok(items);
    }

    [Function("ApproveSuggestion")]
    public async Task<IResult> ApproveAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mod/suggestions/{id:guid}/approve")] HttpRequest req,
        Guid id,
        CancellationToken ct
    )
    {
        if (!admin.IsAdmin(req.HttpContext))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var updated = await store.SetStatusAsync(id, SuggestionStatus.Approved, ct);
        if (updated is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation("Suggestion {Id} approved.", id);
        return Results.Ok(updated);
    }

    [Function("RejectSuggestion")]
    public async Task<IResult> RejectAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mod/suggestions/{id:guid}/reject")] HttpRequest req,
        Guid id,
        CancellationToken ct
    )
    {
        if (!admin.IsAdmin(req.HttpContext))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var updated = await store.SetStatusAsync(id, SuggestionStatus.Rejected, ct);
        if (updated is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation("Suggestion {Id} rejected.", id);
        return Results.Ok(updated);
    }

    [Function("PurgeRejected")]
    public async Task<IResult> PurgeRejectedAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mod/purge-rejected")] HttpRequest req,
        CancellationToken ct
    )
    {
        if (!admin.IsAdmin(req.HttpContext))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var days = DefaultPurgeAgeDays;
        if (req.Query.TryGetValue("olderThanDays", out var raw)
            && int.TryParse(raw.ToString(), out var parsed)
            && parsed >= 0)
        {
            days = parsed;
        }

        var count = await store.PurgeRejectedOlderThanAsync(TimeSpan.FromDays(days), ct);
        logger.LogInformation("Purged {Count} rejected suggestions older than {Days} days.", count, days);
        return Results.Ok(new PurgeRejectedResponse(count));
    }
}
