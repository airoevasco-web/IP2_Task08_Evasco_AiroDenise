namespace Application.Features.Auth.Login;

using Application.Abstractions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class LoginCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Email == email, cancellationToken);

        // Use the same error for an unknown email and an incorrect password.
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result<LoginResponse>.Failure(Error.Unauthorized(
                "Auth.InvalidCredentials", "Invalid email or password."));
        }

        user.LastLoginAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            tokenService.GenerateAccessToken(user),
            tokenService.GetAccessTokenExpiry(),
            user.Role.ToString(),
            $"{user.FirstName} {user.LastName}"));
    }
}