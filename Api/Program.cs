
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Api.Features.Auth;
using Api.Features.Students;
using Api.Features.Courses;
using Api.Features.Admissions;
using Application.Abstractions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Auth.Register;
using Application.Features.Auth.Login;
using Application.Features.Students.CreateStudent;
using Application.Features.Students.GetAllStudents;
using Application.Features.Students.GetStudentById;
using Application.Features.Courses.CreateCourse;
using Application.Features.Courses.GetAllCourses;
using Application.Features.Courses.GetCourseById;
using Application.Features.Admissions.CreateAdmission;
using Application.Features.Admissions.GetAdmissionById;
using Domain.Common;
using Domain.Entities;
using FluentValidation;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<IAppDbContext, AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

// Password Hasher
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// JWT Options
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        o => !string.IsNullOrWhiteSpace(o.SigningKey),
        "Jwt:SigningKey is required. Set it using User Secrets.")
    .Validate(
        o => Encoding.UTF8.GetByteCount(o.SigningKey ?? "") >= 32,
        "Jwt:SigningKey must be at least 256 bits (32 characters).")
    .ValidateOnStart();

// JWT Token Service
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// Token Cleanup Background Service
builder.Services.AddHostedService<TokenCleanupService>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<IAppDbContext>();

                var cancellationToken = context.HttpContext.RequestAborted;

                // Check single-token revocation
                var jti = context.Principal?
                    .FindFirstValue(JwtRegisteredClaimNames.Jti);

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Token ID is missing.");
                    return;
                }

                var isRevoked = await dbContext.RevokedTokens
                    .AnyAsync(r => r.Jti == jti, cancellationToken);

                if (isRevoked)
                {
                    context.Fail("This access token has been revoked.");
                    return;
                }

                // Check logout-all timestamp
                var sub = context.Principal?
                    .FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                var iat = context.Principal?
                    .FindFirstValue(JwtRegisteredClaimNames.Iat);

                if (!Guid.TryParse(sub, out var userId) ||
                    !long.TryParse(iat, out var issuedAtUnix))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var appUser = await dbContext.Users
                    .FirstOrDefaultAsync(
                        u => u.Id == userId,
                        cancellationToken);

                if (appUser is null)
                {
                    context.Fail("User account not found.");
                    return;
                }

                var issuedAtUtc = DateTimeOffset
                    .FromUnixTimeSeconds(issuedAtUnix)
                    .UtcDateTime;

                if (appUser.TokensValidFromUtc is DateTime validFromUtc &&
                    issuedAtUtc < validFromUtc)
                {
                    context.Fail("This session has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentValidator>();

// Handlers
builder.Services.AddScoped<ICommandHandler<RegisterCommand, Result<RegisterResponse>>, RegisterCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, Result<LoginResponse>>, LoginCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CreateStudentCommand, Result<CreateStudentResponse>>, CreateStudentCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAllStudentsQuery, Result<GetAllStudentsResponse>>, GetAllStudentsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>>, GetStudentByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, Result<CreateCourseResponse>>, CreateCourseCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAllCoursesQuery, Result<GetAllCoursesResponse>>, GetAllCoursesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetCourseByIdQuery, Result<GetCourseByIdResponse>>, GetCourseByIdQueryHandler>();
builder.Services.AddScoped<ICommandHandler<CreateAdmissionCommand, Result<CreateAdmissionResponse>>, CreateAdmissionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetAdmissionByIdQuery, Result<GetAdmissionByIdResponse>>, GetAdmissionByIdQueryHandler>();

// API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Vertical Slice API";
        document.Info.Description = "A simple API demonstrating Vertical Slice Architecture";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the access token from /api/auth/login"
            };

        document.Security ??= [];
        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Seed data
await app.Services.MigrateAndSeedAsync();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Root endpoint
app.MapGet("/", () => "Welcome to the Vertical Slice API! Go to /scalar/v1 for API documentation.");

// Map endpoints
app.MapAuthEndpoints();
app.MapStudentEndpoints();
app.MapCourseEndpoints();
app.MapAdmissionEndpoints();

app.Run();
