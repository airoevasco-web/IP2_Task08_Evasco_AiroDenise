
namespace Application.Features.Auth.Login;

using Application.Abstractions.Messaging;
using Domain.Common;

public sealed record LoginCommand(string Email, string Password)
    : ICommand<Result<LoginResponse>>;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Role,
    string FullName);
