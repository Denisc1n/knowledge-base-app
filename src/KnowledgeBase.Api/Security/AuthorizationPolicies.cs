namespace KnowledgeBase.Api.Security;

public static class AuthorizationPolicies
{
    public const string AuthenticatedUser = nameof(AuthenticatedUser);
    public const string AdminOnly = nameof(AdminOnly);
}
