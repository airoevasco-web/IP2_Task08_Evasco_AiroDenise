# Vertical Slice Architecture — Student Management API

A simple API demonstrating Vertical Slice Architecture in .NET 10, built for educational purposes.

---

## Tech Stack

| Component      | Technology                          |
| -------------- | ----------------------------------- |
| Framework      | .NET 10                             |
| Architecture   | Vertical Slice + Clean Architecture |
| ORM            | EF Core 10                          |
| Database       | InMemory (for demo)                 |
| Validation     | FluentValidation                    |
| CQRS           | Custom ICommand/IQuery (No MediatR) |
| API Docs       | Scalar (No Swashbuckle)             |
| Result Pattern | Domain.Common.Result\<T\>           |

---

## Project Structure

```
VerticalSlice/
├── VerticalSlice.sln
├── Domain/                          # Entities (ZERO dependencies)
│   ├── Common/
│   │   ├── BaseEntity.cs           # Base class with Guid Id
│   │   └── Result.cs               # Result<T> pattern
│   └── Entities/
│       ├── Student.cs
│       ├── Course.cs
│       ├── Admission.cs
│       └── AdmissionCourse.cs
├── Application/                     # Handlers, Validators, Abstractions
│   ├── Abstractions/
│   │   ├── Data/
│   │   │   └── IAppDbContext.cs    # Database abstraction
│   │   └── Messaging/
│   │       ├── ICommand.cs         # Write operations interface
│   │       ├── ICommandHandler.cs  # Command handler interface
│   │       ├── IQuery.cs           # Read operations interface
│   │       └── IQueryHandler.cs    # Query handler interface
│   └── Features/
│       ├── Students/
│       │   ├── CreateStudent/
│       │   │   ├── CreateStudentCommand.cs
│       │   │   ├── CreateStudentCommandHandler.cs
│       │   │   ├── CreateStudentValidator.cs
│       │   │   └── CreateStudentResponse.cs
│       │   └── GetStudentById/
│       │       ├── GetStudentByIdQuery.cs
│       │       ├── GetStudentByIdQueryHandler.cs
│       │       └── GetStudentByIdResponse.cs
│       ├── Courses/
│       │   ├── CreateCourse/
│       │   │   ├── CreateCourseCommand.cs
│       │   │   ├── CreateCourseCommandHandler.cs
│       │   │   ├── CreateCourseValidator.cs
│       │   │   └── CreateCourseResponse.cs
│       │   └── GetCourseById/
│       │       ├── GetCourseByIdQuery.cs
│       │       ├── GetCourseByIdQueryHandler.cs
│       │       └── GetCourseByIdResponse.cs
│       └── Admissions/
│           ├── CreateAdmission/
│           │   ├── CreateAdmissionCommand.cs
│           │   ├── CreateAdmissionCommandHandler.cs
│           │   ├── CreateAdmissionValidator.cs
│           │   └── CreateAdmissionResponse.cs
│           └── GetAdmissionById/
│               ├── GetAdmissionByIdQuery.cs
│               ├── GetAdmissionByIdQueryHandler.cs
│               └── GetAdmissionByIdResponse.cs
├── Infrastructure/                  # EF Core, Persistence
│   └── Persistence/
│       └── AppDbContext.cs         # EF Core context
└── Api/                             # Endpoints, DI, Extensions
    ├── Features/
    │   ├── Students/
    │   │   └── StudentEndpoints.cs
    │   ├── Courses/
    │   │   └── CourseEndpoints.cs
    │   └── Admissions/
    │       └── AdmissionEndpoints.cs
    ├── Common/
    │   └── Extensions/
    │       └── ResultExtensions.cs
    └── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- IDE: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider

### Installation

1. Clone the repository or navigate to the project folder:

   ```bash
   cd VerticalSlice
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Run the API:

   ```bash
   dotnet run --project Api
   ```

4. Open Scalar UI in your browser:
   ```
   https://localhost:5205/scalar/v1
   ```

---

## API Endpoints

### Students

| Method | Endpoint             | Description          | Request Body                                  |
| ------ | -------------------- | -------------------- | --------------------------------------------- |
| POST   | `/api/students`      | Create a new student | `{ firstName, lastName, email, dateOfBirth }` |
| GET    | `/api/students/{id}` | Get student by ID    | —                                             |

### Courses

| Method | Endpoint            | Description         | Request Body               |
| ------ | ------------------- | ------------------- | -------------------------- |
| POST   | `/api/courses`      | Create a new course | `{ title, code, credits }` |
| GET    | `/api/courses/{id}` | Get course by ID    | —                          |

### Admissions

| Method | Endpoint               | Description                      | Request Body                               |
| ------ | ---------------------- | -------------------------------- | ------------------------------------------ |
| POST   | `/api/admissions`      | Create admission with courses    | `{ studentId, academicYear, courseIds[] }` |
| GET    | `/api/admissions/{id}` | Get admission by ID with courses | —                                          |

---

## Example Requests

### Create Student

```json
POST /api/students
{
  "firstName": "John",
  "lastName": "Santos",
  "email": "john.doe@ccdi.com",
  "dateOfBirth": "2000-01-15"
}
```

### Create Course

```json
POST /api/courses
{
  "title": "Introduction to Programming",
  "code": "CS101",
  "credits": 3
}
```

### Create Admission

```json
POST /api/admissions
{
  "studentId": "YOUR_STUDENT_ID",
  "academicYear": "2026-2027",
  "courseIds": [
    "COURSE_ID_1",
    "COURSE_ID_2"
  ]
}
```

---

## Architecture Rules

1. **Domain has ZERO external dependencies**
2. **All data access through DbContext** — No repository pattern
3. **Result\<T\> for business errors** — Not exceptions
4. **Records for DTOs** — Primary constructors for DI
5. **Always pass CancellationToken** through async chains

---

## Key Concepts

### Vertical Slice Architecture

Code is organized by feature (CreateStudent, GetCourseById), not by technical layer (Controllers, Services, Repositories). Each feature is self-contained in its own folder.

### Result Pattern

Instead of throwing exceptions, we return Result objects:

```csharp
// Success
return Result<T>.Success(response);

// Failure
return Result<T>.Failure(Error.NotFound("Code", "Message"));
```

### Custom CQRS

Simple interfaces without external dependencies:

- `ICommand<TResponse>` — for write operations
- `IQuery<TResponse>` — for read operations
- `ICommandHandler<TCommand, TResponse>` — handles commands
- `IQueryHandler<TQuery, TResponse>` — handles queries

### FluentValidation

Validates input before it reaches the handler:

```csharp
public sealed class CreateStudentValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

---

## Seed Data

The API automatically seeds the following data on startup:

**Students:**

- John Doe (john.doe@example.com)
- Jane Smith (jane.smith@example.com)

**Courses:**

- Introduction to Programming (CS101, 3 credits)
- Data Structures (CS201, 4 credits)
- Database Systems (CS301, 3 credits)

---

## Common Mistakes to Avoid

1. **Don't use MediatR** — This project uses custom ICommand/IQuery
2. **Don't throw exceptions** — Return Result.Failure() instead
3. **Don't skip validation** — Always add a validator
4. **Don't forget to register** — Add `app.Map{FeatureGroup}Endpoints()` in Program.cs
5. **Don't put business logic in endpoints** — Handlers do the work
6. **Don't use controllers** — This project uses Minimal APIs
7. **Don't use Repository pattern** — Use DbContext directly

---

## Resources

- [Vertical Slice Architecture by Jimmy Bogard](https://www.jimmybogard.com/vertical-slice-architecture/)
- [CodeWithMukesh Clean Architecture Template](https://github.com/iammukeshm/CleanArchitecture.WebApi)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [Minimal APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Scalar API Documentation](https://scalar.com/)

---

## License

This project is for educational purposes.

---

**Developed by Elvin Manuel R. Luces, MIT**
#   I P 2 _ T a s k 0 8 _ E v a s c o _ A i r o D e n i s e  
 