namespace SimpleSubmit.Shared.Contracts;

public enum AdminStatus
{
    Pending = 0,
    Active = 1,
}

public sealed record AdminEntry
(
    string PrincipalId,
    string DisplayName,
    string IdentityProvider,
    AdminStatus Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    string? ActivatedByPrincipalId,
    bool IsBootstrap
);

public sealed record AdminSelfStatus
(
    bool IsSignedIn,
    bool IsAdmin,
    string? PrincipalId,
    string? DisplayName,
    string? IdentityProvider,
    AdminStatus? RequestStatus
);
