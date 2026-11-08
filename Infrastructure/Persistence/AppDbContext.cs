
namespace Infrastructure.Persistence;

using Application.Abstractions.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<AdmissionCourse> AdmissionCourses => Set<AdmissionCourse>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Email).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Admission>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AcademicYear).IsRequired().HasMaxLength(20);
            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId);
        });

        modelBuilder.Entity<AdmissionCourse>(entity =>
        {
            entity.HasKey(ac => ac.Id);
            entity.HasOne(ac => ac.Admission)
                  .WithMany(a => a.AdmissionCourses)
                  .HasForeignKey(ac => ac.AdmissionId);
            entity.HasOne(ac => ac.Course)
                  .WithMany()
                  .HasForeignKey(ac => ac.CourseId);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).IsRequired().HasConversion<int>();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Student)
                  .WithMany()
                  .HasForeignKey(u => u.StudentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.HasKey(r => r.Jti);
        });
    }
}
