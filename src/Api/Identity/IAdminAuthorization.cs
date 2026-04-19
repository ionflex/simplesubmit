using Microsoft.AspNetCore.Http;

namespace SimpleSubmit.Api.Identity;

public interface IAdminAuthorization
{
    bool IsAdmin(HttpContext ctx);
}
