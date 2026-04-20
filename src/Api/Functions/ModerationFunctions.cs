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
    IAdminRegistry admins,
    ILogger<ModerationFunctions> logger
)
{
    private const int DefaultPurgeAgeDays = 30;

    [Function("ListPending")]
    public async Task<IResult> ListPendingAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mod/suggestions/pending")] HttpRequest req,
        CancellationToken ct
    )
    {
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
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
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
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
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
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
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
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

    [Function("AdminSelf")]
    public async Task<IResult> AdminSelfAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mod/self")] HttpRequest req,
        CancellationToken ct
    )
    {
        var principal = admin.GetPrincipal(req.HttpContext);
        if (principal is null)
        {
            return Results.Ok(new AdminSelfStatus(false, false, null, null, null, null));
        }

        var isAdmin = await admin.IsAdminAsync(req.HttpContext, ct);
        var entry = await admins.GetAsync(principal.UserId, ct);
        return Results.Ok(new AdminSelfStatus
        (
            IsSignedIn: true,
            IsAdmin: isAdmin,
            PrincipalId: principal.UserId,
            DisplayName: principal.UserDetails,
            IdentityProvider: principal.IdentityProvider,
            RequestStatus: entry?.Status
        ));
    }

    [Function("RequestAdminAccess")]
    public async Task<IResult> RequestAdminAccessAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mod/admin-requests")] HttpRequest req,
        CancellationToken ct
    )
    {
        var principal = admin.GetPrincipal(req.HttpContext);
        if (principal is null)
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        var entry = await admins.UpsertPendingAsync(principal.UserId, principal.UserDetails, principal.IdentityProvider, ct);
        logger.LogInformation("Admin access requested by {User} ({Principal}), status={Status}", principal.UserDetails, principal.UserId, entry.Status);
        return Results.Ok(entry);
    }

    [Function("ListAdmins")]
    public async Task<IResult> ListAdminsAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mod/admins")] HttpRequest req,
        CancellationToken ct
    )
    {
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var list = await admins.ListAsync(ct);
        return Results.Ok(list);
    }

    [Function("ApproveAdmin")]
    public async Task<IResult> ApproveAdminAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mod/admins/{principalId}/approve")] HttpRequest req,
        string principalId,
        CancellationToken ct
    )
    {
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var approver = admin.GetPrincipal(req.HttpContext);
        var updated = await admins.ApproveAsync(principalId, approver?.UserId ?? "", ct);
        if (updated is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation("Admin {Principal} approved by {Approver}", principalId, approver?.UserDetails ?? "unknown");
        return Results.Ok(updated);
    }

    [Function("RemoveAdmin")]
    public async Task<IResult> RemoveAdminAsync
    (
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "mod/admins/{principalId}")] HttpRequest req,
        string principalId,
        CancellationToken ct
    )
    {
        if (!await admin.IsAdminAsync(req.HttpContext, ct))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (string.Equals(principalId, admin.BootstrapPrincipalId, StringComparison.Ordinal))
        {
            return Results.BadRequest(new { error = "cannot remove the bootstrap admin" });
        }

        var removed = await admins.RemoveAsync(principalId, ct);
        if (!removed)
        {
            return Results.NotFound();
        }

        logger.LogInformation("Admin/request {Principal} removed.", principalId);
        return Results.NoContent();
    }
}
