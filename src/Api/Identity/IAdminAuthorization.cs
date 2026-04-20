using Microsoft.AspNetCore.Http;

namespace SimpleSubmit.Api.Identity;

public interface IAdminAuthorization
{
    ValueTask<bool> IsAdminAsync(HttpContext ctx, CancellationToken ct = default);
    ClientPrincipal? GetPrincipal(HttpContext ctx);
    string? BootstrapPrincipalId { get; }
}
