namespace Infrastructure.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class DbSeeder
{
    // Fixed IDs so your saved requests keep working after a database reset
    private static readonly Guid JohnDoeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JaneSmithId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Cs101Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Cs201Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Cs301Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
public static async Task MigrateAndSeedAsync(
    this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Students.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded - skipping.");
            return;
        }
        logger.LogInformation("Seeding initial data...");
        db.Students.AddRange(
        new Student
        {
            Id = JohnDoeId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DateOfBirth = new DateTime(2000, 1, 15)
        },
        new Student
        {
            Id = JaneSmithId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            DateOfBirth = new DateTime(2001, 5, 20)
        });
        db.Courses.AddRange(
        new Course
        {
            Id = Cs101Id,
            Title = "Introduction to Programming",
            Code = "CS101",
            Credits = 3
        },
        new Course
        {
            Id = Cs201Id,
            Title = "Data Structures",
            Code = "CS201",
            Credits = 4
        },
        new Course
        {
            Id = Cs301Id,
            Title = "Database Systems",
            Code = "CS301",
            Credits = 3
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeding complete.");
    }
}