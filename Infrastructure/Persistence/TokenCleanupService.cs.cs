
namespace Infrastructure.Persistence;

using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class TokenCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            using var scope = scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<IAppDbContext>();

            var now = timeProvider.GetUtcNow().UtcDateTime;

            await dbContext.RevokedTokens
                .Where(token => token.ExpiresAtUtc <= now)
                .ExecuteDeleteAsync(stoppingToken);
        }
    }
}
