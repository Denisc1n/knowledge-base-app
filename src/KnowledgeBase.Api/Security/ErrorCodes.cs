namespace KnowledgeBase.Api.Security;

public static class ErrorCodes
{
    public const string AuthRateLimited = "auth.rate_limited";
    public const string AuthDuplicateUser = "auth.duplicate_user";
    public const string AuthInvalidCredentials = "auth.invalid_credentials";
    public const string AuthRefreshTokenMissing = "auth.refresh_token_missing";
    public const string AuthMissingUserId = "auth.missing_user_id";
    public const string AuthUnauthorized = "auth.unauthorized";
    public const string AuthForbidden = "auth.forbidden";
    public const string AdminInvalidOperation = "admin.invalid_operation";
    public const string NotesNotFound = "notes.not_found";
    public const string UsersNotFound = "users.not_found";
}
