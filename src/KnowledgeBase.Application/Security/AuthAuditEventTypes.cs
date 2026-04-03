namespace KnowledgeBase.Application.Security;

public static class AuthAuditEventTypes
{
    public const string Login = "login";
    public const string Refresh = "refresh";
    public const string Logout = "logout";
    public const string LogoutAll = "logout_all";
    public const string ResetPassword = "reset_password";
}
