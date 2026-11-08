namespace Application.Features.Auth.Register;

using FluentValidation;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12);

        RuleFor(x => x.FirstName)
            .NotEmpty();

        RuleFor(x => x.LastName)
            .NotEmpty();

        RuleFor(x => x.Role)
            .Must(r => r is null ||
                Enum.TryParse<Domain.Enums.UserRole>(r, true, out _))
            .WithMessage("Role must be Student, Instructor, Registrar, or Admin.");
    }
}