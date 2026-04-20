using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleSubmit.Api.Storage;

namespace SimpleSubmit.Api.Identity;

public sealed class SwaAdminAuthorization
(
    IConfiguration config,
    IAdminRegistry registry,
    ILogger<SwaAdminAuthorization> logger
) : IAdminAuthorization
{
    private const string PrincipalHeader = "x-ms-client-principal";
    private readonly string? _bootstrapPrincipalId = config["ADMIN_PRINCIPAL_ID"];

    public string? BootstrapPrincipalId => _bootstrapPrincipalId;

    public async ValueTask<bool> IsAdminAsync(HttpContext ctx, CancellationToken ct = default)
    {
        var principal = GetPrincipal(ctx);
        if (principal is null)
        {
            if (string.IsNullOrEmpty(_bootstrapPrincipalId))
            {
                logger.LogWarning("ADMIN_PRINCIPAL_ID is not configured; admin routes are open.");
                return true;
            }
            return false;
        }

        if (string.Equals(principal.UserId, _bootstrapPrincipalId, StringComparison.Ordinal))
        {
            return true;
        }

        return await registry.IsActiveAsync(principal.UserId, ct);
    }

    public ClientPrincipal? GetPrincipal(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(PrincipalHeader, out var headerValues))
        {
            return null;
        }

        var encoded = headerValues.ToString();
        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return JsonSerializer.Deserialize<WireClientPrincipal>(json, JsonOpts) is { UserId: not null } wire
                ? new ClientPrincipal
                (
                    IdentityProvider: wire.IdentityProvider ?? "",
                    UserId: wire.UserId,
                    UserDetails: wire.UserDetails ?? "",
                    UserRoles: wire.UserRoles ?? []
                )
                : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode {Header}.", PrincipalHeader);
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record WireClientPrincipal
    (
        [property: JsonPropertyName("identityProvider")] string? IdentityProvider,
        [property: JsonPropertyName("userId")] string? UserId,
        [property: JsonPropertyName("userDetails")] string? UserDetails,
        [property: JsonPropertyName("userRoles")] IReadOnlyList<string>? UserRoles
    );
}
