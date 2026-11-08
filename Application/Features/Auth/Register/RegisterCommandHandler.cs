namespace Application.Features.Auth.Register;

using Application.Abstractions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class RegisterCommandHandler(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var exists = await dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
        {
            return Result<RegisterResponse>.Failure(Error.Conflict(
                "Auth.EmailAlreadyRegistered",
                "An account with this email address already exists."));
        }

        var role = Enum.TryParse<UserRole>(
            command.Role, true, out var parsedRole)
            ? parsedRole
            : UserRole.Student;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(command.Password),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Role = role,
            IsActive = true
        };

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (student is not null)
        {
            user.StudentId = student.Id;
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString()));
    }
}