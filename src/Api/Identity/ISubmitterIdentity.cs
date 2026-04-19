using Microsoft.AspNetCore.Http;

namespace SimpleSubmit.Api.Identity;

public readonly record struct SubmitterId(string Value)
{
    public override string ToString() => Value;
}

public interface ISubmitterIdentity
{
    ValueTask<SubmitterId> GetOrCreateAsync(HttpContext ctx);
}
