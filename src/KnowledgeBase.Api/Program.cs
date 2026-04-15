using FluentValidation.AspNetCore;
using KnowledgeBase.Api.HealthChecks;
using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Observability;
using KnowledgeBase.Api.Security;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Infrastructure.DependencyInjection;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.ParentId;
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            []
        }
    });
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running."))
    .AddCheck<MongoHealthCheck>("mongo", timeout: TimeSpan.FromSeconds(5));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.Headers[ApiVersioningConventions.SupportedVersionsHeader] = "1.0";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Too many authentication attempts. Please wait before trying again.",
            Type = "https://httpstatuses.com/429",
            Instance = context.HttpContext.Request.Path
        }.WithCode(ErrorCodes.AuthRateLimited), cancellationToken);
    };

    options.AddPolicy(RateLimitPolicies.AuthSensitive, httpContext =>
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        var ipAddress = string.IsNullOrWhiteSpace(forwardedFor)
            ? httpContext.Connection.RemoteIpAddress?.ToString()
            : forwardedFor;
        var partitionKey = string.IsNullOrWhiteSpace(ipAddress)
            ? "unknown"
            : ipAddress;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KnowledgeBase.Api.Security.Jwt");

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers[ApiVersioningConventions.SupportedVersionsHeader] = "1.0";

                logger.LogWarning(
                    "JWT challenge issued for {Method} {Path}. Error: {Error}; Description: {Description}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Error,
                    context.ErrorDescription);

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = string.IsNullOrWhiteSpace(context.ErrorDescription)
                        ? "Authentication is required to access this resource."
                        : context.ErrorDescription,
                    Type = "https://httpstatuses.com/401",
                    Instance = context.Request.Path
                }.WithCode(ErrorCodes.AuthUnauthorized));
            },
            OnForbidden = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KnowledgeBase.Api.Security.Jwt");

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers[ApiVersioningConventions.SupportedVersionsHeader] = "1.0";

                logger.LogWarning(
                    "JWT forbidden response for {Method} {Path} and user {UserId}.",
                    context.Request.Method,
                    context.Request.Path,
                    context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.Principal?.FindFirstValue("sub"));

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to access this resource.",
                    Type = "https://httpstatuses.com/403",
                    Instance = context.Request.Path
                }.WithCode(ErrorCodes.AuthForbidden));
            },
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KnowledgeBase.Api.Security.Jwt");
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("sub");
                var tokenSecurityStamp = context.Principal?.FindFirstValue("sstamp");

                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    logger.LogWarning("JWT validation failed due to missing required claims.");
                    context.Fail("Invalid token.");
                    return;
                }

                var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);

                if (user is null || !user.IsActive || user.SecurityStamp != tokenSecurityStamp)
                {
                    logger.LogWarning(
                        "JWT validation failed for user {UserId}. UserExists: {UserExists}; IsActive: {IsActive}; SecurityStampMatches: {SecurityStampMatches}.",
                        userId,
                        user is not null,
                        user?.IsActive,
                        user?.SecurityStamp == tokenSecurityStamp);
                    context.Fail("Invalid token.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.NameIdentifier);
    });

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.NameIdentifier);
        policy.RequireAssertion(context =>
            context.User.IsInRole(UserRole.Admin.ToString()) ||
            context.User.IsInRole(UserRole.MasterAdmin.ToString()));
    });

    options.AddPolicy(AuthorizationPolicies.MasterAdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.NameIdentifier);
        policy.RequireRole(UserRole.MasterAdmin.ToString());
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await EnsureBootstrapMasterAdminAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers[ApiVersioningConventions.SupportedVersionsHeader] = "1.0";
    await next();
});
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self",
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name is "self" or "mongo",
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapControllers();

app.Run();

static async Task EnsureBootstrapMasterAdminAsync(
    IServiceProvider services,
    ILogger logger,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var settings = scope.ServiceProvider
        .GetRequiredService<IOptions<BootstrapAdminSettings>>()
        .Value;

    if (!settings.Enabled)
        return;

    if (string.IsNullOrWhiteSpace(settings.FirstName) ||
        string.IsNullOrWhiteSpace(settings.LastName) ||
        string.IsNullOrWhiteSpace(settings.Username) ||
        string.IsNullOrWhiteSpace(settings.Email) ||
        string.IsNullOrWhiteSpace(settings.Password))
    {
        throw new InvalidOperationException("Bootstrap admin settings are incomplete.");
    }

    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var existingMasterAdmin = await userRepository.GetByRoleAsync(UserRole.MasterAdmin, cancellationToken);
    if (existingMasterAdmin is not null)
        return;

    var normalizedUsername = User.NormalizeUsername(settings.Username);
    var normalizedEmail = User.NormalizeEmail(settings.Email);

    var existingByUsername = await userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);
    var existingByEmail = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

    if (existingByUsername is not null && existingByEmail is not null && existingByUsername.Id != existingByEmail.Id)
        throw new InvalidOperationException("Bootstrap admin username and email refer to different users.");

    var bootstrapUser = existingByUsername ?? existingByEmail;

    if (bootstrapUser is null)
    {
        bootstrapUser = User.Create(
            settings.FirstName,
            settings.LastName,
            settings.Username,
            settings.Email,
            passwordHasher.Hash(settings.Password),
            DateTime.UtcNow,
            UserRole.MasterAdmin);

        await userRepository.CreateAsync(bootstrapUser, cancellationToken);
        logger.LogInformation("Bootstrap master admin {Username} was created.", bootstrapUser.Username);
        return;
    }

    bootstrapUser.FirstName = settings.FirstName.Trim();
    bootstrapUser.LastName = settings.LastName.Trim();
    bootstrapUser.Username = normalizedUsername;
    bootstrapUser.Email = normalizedEmail;
    bootstrapUser.PasswordHash = passwordHasher.Hash(settings.Password);
    bootstrapUser.IsActive = true;
    bootstrapUser.Role = UserRole.MasterAdmin;
    bootstrapUser.RotateSecurityStamp();

    await userRepository.UpdateAsync(bootstrapUser, cancellationToken);
    logger.LogInformation("Existing user {Username} was promoted to bootstrap master admin.", bootstrapUser.Username);
}

public partial class Program;
