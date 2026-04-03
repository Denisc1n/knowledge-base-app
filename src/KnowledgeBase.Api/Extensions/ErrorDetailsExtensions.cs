using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Extensions;

public static class ErrorDetailsExtensions
{
    public static ProblemDetails WithCode(this ProblemDetails error, string code)
    {
        error.Extensions["code"] = code;
        return error;
    }

    public static ObjectResult ConflictError(
        this ControllerBase controller,
        string detail,
        string code,
        string? type = null,
        string? field = null)
    {
        var error = CreateErrorDetails(
            controller,
            StatusCodes.Status409Conflict,
            "Conflict",
            detail,
            code,
            type);

        if (!string.IsNullOrWhiteSpace(field))
            error.Extensions["field"] = field;

        return new ObjectResult(error)
        {
            StatusCode = error.Status
        };
    }

    public static ObjectResult UnauthorizedError(
        this ControllerBase controller,
        string detail,
        string code,
        string? type = null)
    {
        var error = CreateErrorDetails(
            controller,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            detail,
            code,
            type);

        return new ObjectResult(error)
        {
            StatusCode = error.Status
        };
    }

    public static ObjectResult NotFoundError(
        this ControllerBase controller,
        string detail,
        string code,
        string? type = null)
    {
        var error = CreateErrorDetails(
            controller,
            StatusCodes.Status404NotFound,
            "Not Found",
            detail,
            code,
            type);

        return new ObjectResult(error)
        {
            StatusCode = error.Status
        };
    }

    public static ProblemDetails CreateErrorDetails(
        this ControllerBase controller,
        int statusCode,
        string title,
        string detail,
        string code,
        string? type = null)
    {
        var error = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = controller.HttpContext.Request.Path
        };

        return error.WithCode(code);
    }
}
