
namespace Api.Features.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Common.Extensions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Auth.Register;
using Application.Features.Auth.Login;
using Domain.Common;
using Domain.Entities;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create a new user account")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Sign in and receive an access token")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Revoke the current access token");

        group.MapPost("/logout-all", LogoutAll)
            .RequireAuthorization()
            .WithName("LogoutAll")
            .WithSummary("Revoke every session for the current user");
    }

    private static async Task<IResult> Register(
        RegisterCommand command,
        ICommandHandler<RegisterCommand, Result<RegisterResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/auth/users/{result.Value!.UserId}", result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Login(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<LoginResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Logout(
        ClaimsPrincipal user,
        IAppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expRaw = user.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (jti is null || expRaw is null || !long.TryParse(expRaw, out var expUnix))
        {
            return TypedResults.BadRequest();
        }

        dbContext.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> LogoutAll(
        ClaimsPrincipal user,
        IAppDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var appUser = await dbContext.Users.FindAsync(
            [userId], cancellationToken);

        if (appUser is null)
        {
            return TypedResults.Unauthorized();
        }

        appUser.TokensValidFromUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
