namespace SimpleSubmit.Shared.Contracts;

public sealed record Suggestion
(
    Guid Id,
    string Text,
    string? AuthorName,
    DateTimeOffset SubmittedAtUtc
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
