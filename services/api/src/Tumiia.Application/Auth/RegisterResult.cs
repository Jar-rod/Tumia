namespace Tumiia.Application.Auth;

/// <summary>The outcome of registering: a new session, or why it failed.</summary>
public sealed record RegisterResult(AuthSession? Session, bool DuplicateEmail, IReadOnlyDictionary<string, string[]> Errors);
