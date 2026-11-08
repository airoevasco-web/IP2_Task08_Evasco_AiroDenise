namespace Application.Features.Auth.Register;

using Application.Abstractions.Messaging;
using Domain.Common;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Role = null
) : ICommand<Result<RegisterResponse>>;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);