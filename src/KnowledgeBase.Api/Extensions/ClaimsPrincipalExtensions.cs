using System.Security.Claims;

namespace KnowledgeBase.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetCurrentUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
        principal.FindFirstValue("sub");
}
