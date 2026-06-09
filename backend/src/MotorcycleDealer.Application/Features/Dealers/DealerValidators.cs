using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Application.Features.Auth;

namespace MotorcycleDealer.Application.Features.Dealers;

public static partial class DealerValidationPatterns
{
    private static readonly string[] AcceptedTimeFormats = ["HH:mm", "HH:mm:ss", "H:mm", "H:mm:ss"];

    [GeneratedRegex(@"^DLR-[A-Z0-9-]{2,16}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex DealerCodeRegex();

    [GeneratedRegex(@"^\+?[0-9]{8,15}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex BranchPhoneRegex();

    [GeneratedRegex(@"^EMP-[A-Z0-9-]{3,25}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex StaffEmployeeCodeRegex();

    public static bool TryParseWorkshopTime(string? raw, out TimeOnly time)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            time = default;
            return false;
        }

        return TimeOnly.TryParseExact(
            raw.Trim(),
            AcceptedTimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
    }
}

public sealed class UpsertDealerRequestValidator : AbstractValidator<UpsertDealerRequest>
{
    public UpsertDealerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Dealership branch code is required.")
            .MaximumLength(20).WithMessage("Dealership code must not exceed 20 characters.")
            .Must(code => DealerValidationPatterns.DealerCodeRegex().IsMatch(code.Trim().ToUpperInvariant()))
            .WithMessage("Dealership code must match format 'DLR-...' (uppercase alphanumeric/hyphens, up to 20 chars).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dealership name is required.")
            .Must(n => n.Trim().Length >= 2).WithMessage("Dealership name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Dealership name must not exceed 150 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .Must(c => c.Trim().Length >= 2).WithMessage("City must be at least 2 characters long.")
            .MaximumLength(80).WithMessage("City must not exceed 80 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .Must(a => a.Trim().Length >= 5).WithMessage("Address must be at least 5 characters long.")
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .Must(p => DealerValidationPatterns.BranchPhoneRegex().IsMatch(p.Trim()))
            .WithMessage("Phone number must contain 8 to 15 digits (optional leading '+').");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .MaximumLength(256).WithMessage("Contact email must not exceed 256 characters.")
            .Must(email => AuthValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Contact email must be a valid email address.");

        RuleFor(x => x.OpeningTime)
            .NotEmpty().WithMessage("Opening time is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .WithMessage("Opening time must be in HH:mm or HH:mm:ss format.");

        RuleFor(x => x.ClosingTime)
            .NotEmpty().WithMessage("Closing time is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .WithMessage("Closing time must be in HH:mm or HH:mm:ss format.")
            .Must((req, closingRaw) =>
                DealerValidationPatterns.TryParseWorkshopTime(req.OpeningTime, out var open) &&
                DealerValidationPatterns.TryParseWorkshopTime(closingRaw, out var close) &&
                close > open)
            .WithMessage("Closing time must be strictly later than opening time.");

        RuleFor(x => x.DefaultSlotCapacity)
            .InclusiveBetween(1, 50)
            .WithMessage("Default slot capacity must be between 1 and 50 bays.");

        RuleFor(x => x)
            .Must(req => (req.Latitude is null && req.Longitude is null) ||
                         (req.Latitude is >= -90m and <= 90m && req.Longitude is >= -180m and <= 180m))
            .WithName("coordinates")
            .WithMessage("Latitude (-90..90) and Longitude (-180..180) must either both be null or both be valid coordinates.");
    }
}

public sealed class CreateDealerStaffRequestValidator : AbstractValidator<CreateDealerStaffRequest>
{
    public CreateDealerStaffRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Staff email address is required.")
            .MaximumLength(256).WithMessage("Staff email address must not exceed 256 characters.")
            .Must(email => AuthValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Staff email address must be valid.");

        RuleFor(x => x.ResolveEffectivePassword())
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Must(AuthValidationPatterns.HasRequiredPasswordComplexity)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")
            .OverridePropertyName("password");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(n => n.Trim().Length >= 2).WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .Must(p => AuthValidationPatterns.PhoneRegex().IsMatch(p.Trim()))
            .WithMessage("Phone number must contain 9 to 15 digits (optional leading '+').");

        RuleFor(x => x.StaffEmployeeCode)
            .NotEmpty().WithMessage("Staff employee code is required.")
            .MaximumLength(30).WithMessage("Staff employee code must not exceed 30 characters.")
            .Must(c => DealerValidationPatterns.StaffEmployeeCodeRegex().IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Staff employee code must match format 'EMP-...' (3 to 25 uppercase alphanumeric/hyphen characters after prefix).");
    }
}

public sealed class UpdateStaffStatusRequestValidator : AbstractValidator<UpdateStaffStatusRequest>
{
    public UpdateStaffStatusRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.IsActive)
            .NotNull()
            .WithMessage("Account active status (isActive) is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason must not exceed 300 characters.");
    }
}
