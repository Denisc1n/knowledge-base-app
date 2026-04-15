namespace KnowledgeBase.Application.Security;

public static class RefreshSessionRevocationReasons
{
    public const string Logout = "logout";
    public const string Rotated = "rotated";
    public const string LogoutAll = "logout_all";
    public const string PasswordReset = "password_reset";
    public const string RoleChanged = "role_changed";
    public const string UserDeactivated = "user_deactivated";
}
