using System.Text.RegularExpressions;
using FluentValidation;

namespace MotorcycleDealer.Application.Features.Auth;

public static partial class AuthValidationPatterns
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[0-9]{9,15}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex PhoneRegex();

    public static bool HasRequiredPasswordComplexity(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 128)
        {
            return false;
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var ch in password)
        {
            if (char.IsUpper(ch))
            {
                hasUpper = true;
            }
            else if (char.IsLower(ch))
            {
                hasLower = true;
            }
            else if (char.IsDigit(ch))
            {
                hasDigit = true;
            }
            else if (!char.IsWhiteSpace(ch))
            {
                hasSpecial = true;
            }
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}

public sealed class RegisterCustomerRequestValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.")
            .Must(email => AuthValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Email address must be a valid RFC 5322 email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Must(AuthValidationPatterns.HasRequiredPasswordComplexity)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(name => name.Trim().Length >= 2)
            .WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .Must(phone => AuthValidationPatterns.PhoneRegex().IsMatch(phone.Trim()))
            .WithMessage("Phone number must contain 9 to 15 digits (optional leading '+').");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.")
            .Must(email => AuthValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Email address format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MinimumLength(32).WithMessage("Refresh token must be at least 32 characters long.");
    }
}
