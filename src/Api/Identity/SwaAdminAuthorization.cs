using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimpleSubmit.Api.Identity;

public sealed class SwaAdminAuthorization(IConfiguration config, ILogger<SwaAdminAuthorization> logger) : IAdminAuthorization
{
    private const string PrincipalHeader = "x-ms-client-principal";
    private readonly string? _adminUserId = config["ADMIN_PRINCIPAL_ID"];

    public bool IsAdmin(HttpContext ctx)
    {
        if (string.IsNullOrEmpty(_adminUserId))
        {
            // Local dev / misconfigured prod: allow, but log loudly so it's obvious.
            logger.LogWarning("ADMIN_PRINCIPAL_ID is not configured; admin routes are open.");
            return true;
        }

        if (!ctx.Request.Headers.TryGetValue(PrincipalHeader, out var headerValues))
        {
            return false;
        }

        var encoded = headerValues.ToString();
        if (string.IsNullOrEmpty(encoded))
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOpts);
            return principal?.IdentityProvider == "github"
                && string.Equals(principal.UserId, _adminUserId, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode {Header}.", PrincipalHeader);
            return false;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record ClientPrincipal
    (
        [property: JsonPropertyName("identityProvider")] string? IdentityProvider,
        [property: JsonPropertyName("userId")] string? UserId,
        [property: JsonPropertyName("userDetails")] string? UserDetails,
        [property: JsonPropertyName("userRoles")] IReadOnlyList<string>? UserRoles
    );
}
