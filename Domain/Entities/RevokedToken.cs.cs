namespace Domain.Entities;

public sealed class RevokedToken
{
    public string Jti { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}