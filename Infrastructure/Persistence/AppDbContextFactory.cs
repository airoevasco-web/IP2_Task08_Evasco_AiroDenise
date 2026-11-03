namespace Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
// Used ONLY by the dotnet-ef command-line tool. Never runs as part of the API.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Database")
        ?? "Data Source=verticalslice.db";
        var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(connectionString)
        .Options;
        return new AppDbContext(options);
    }
}