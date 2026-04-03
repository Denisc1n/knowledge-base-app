using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<string> RequireCurrentUserId()
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedError(
                "User identifier is missing.",
                code: ErrorCodes.AuthMissingUserId,
                type: "https://httpstatuses.com/401");
        }

        return userId;
    }
}
