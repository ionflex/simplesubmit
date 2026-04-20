namespace SimpleSubmit.Api.Identity;

public sealed record ClientPrincipal
(
    string IdentityProvider,
    string UserId,
    string UserDetails,
    IReadOnlyList<string> UserRoles
);
