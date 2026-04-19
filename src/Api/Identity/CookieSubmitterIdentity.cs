using Microsoft.AspNetCore.Http;

namespace SimpleSubmit.Api.Identity;

public sealed class CookieSubmitterIdentity : ISubmitterIdentity
{
    private const string CookieName = "sid";

    public ValueTask<SubmitterId> GetOrCreateAsync(HttpContext ctx)
    {
        if (ctx.Request.Cookies.TryGetValue(CookieName, out var existing)
            && Guid.TryParseExact(existing, "N", out _))
        {
            return new(new SubmitterId(existing));
        }

        var fresh = Guid.NewGuid().ToString("N");
        ctx.Response.Cookies.Append
        (
            CookieName,
            fresh,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
            }
        );
        return new(new SubmitterId(fresh));
    }
}
