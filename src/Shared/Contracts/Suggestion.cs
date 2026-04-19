namespace SimpleSubmit.Shared.Contracts;

public enum SuggestionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public sealed record Suggestion
(
    Guid Id,
    string Text,
    string? AuthorName,
    DateTimeOffset SubmittedAtUtc,
    SuggestionStatus Status,
    DateTimeOffset? ModeratedAtUtc
);

public sealed record SubmitSuggestionRequest
(
    string Text,
    string? AuthorName
);

public sealed record SubmitSuggestionResponse
(
    Guid Id
);

public sealed record PurgeRejectedResponse
(
    int PurgedCount
);
