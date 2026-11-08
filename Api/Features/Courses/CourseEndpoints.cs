
namespace Api.Features.Courses;

using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Courses.CreateCourse;
using Application.Features.Courses.GetAllCourses;
using Application.Features.Courses.GetCourseById;
using Domain.Common;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses")
            .WithTags("Courses")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCourses")
            .WithSummary("Get all courses");

        group.MapPost("/", Create)
            .WithName("CreateCourse")
            .WithSummary("Create a new course");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCourseById")
            .WithSummary("Get course by ID");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCoursesQuery, Result<GetAllCoursesResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAllCoursesQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCourseCommand command,
        ICommandHandler<CreateCourseCommand, Result<CreateCourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetCourseById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCourseByIdQuery, Result<GetCourseByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetCourseByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
}
