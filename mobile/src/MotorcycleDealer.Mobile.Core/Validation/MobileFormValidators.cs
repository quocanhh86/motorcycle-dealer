using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Mobile.Core.Domain;

namespace MotorcycleDealer.Mobile.Core.Validation;

public static partial class MobileValidationPatterns
{
    public const int MaxMileageKm = 1_000_000;
    public const int MinSymptomLength = 10;
    public const int MaxSymptomLength = 1000;

    public static readonly IReadOnlySet<string> AllowedDefectCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Engine",
        "Transmission",
        "Electrical",
        "BrakesAndSuspension",
        "FrameAndPaint",
        "ExhaustAndFuel"
    };

    public static readonly IReadOnlySet<string> AllowedPaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BankTransfer",
        "Cash",
        "Card",
        "EWallet",
        "Installment"
    };

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

public sealed record LoginInput(
    string Email,
    string Password);

public sealed record RegisterInput(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber);

public sealed record RegisterVehicleInput(
    string Vin,
    Guid MotorcycleModelId,
    int CurrentMileageKm,
    string? LicensePlate = null,
    string? ColorName = null);

public sealed record WarrantyClaimInput(
    Guid CustomerVehicleId,
    Guid DealerId,
    int OdometerAtClaimKm,
    string DefectCategory,
    string SymptomDescription);

public sealed record SalesOrderInput(
    Guid DealerInventoryId,
    string CustomerEmail,
    string PaymentMethod,
    string? LicensePlate = null,
    string? VoucherCode = null,
    string? Notes = null);

public sealed class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.")
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}

public sealed class RegisterInputValidator : AbstractValidator<RegisterInput>
{
    public RegisterInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.")
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Must(MobileValidationPatterns.HasRequiredPasswordComplexity)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(name => name.Trim().Length >= 2).WithMessage("Full name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Vietnam phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .Must(phone => MobileValidationPatterns.PhoneRegex().IsMatch(phone.Trim()))
            .WithMessage("Phone number must contain 9 to 15 digits (optional leading '+').");
    }
}

public sealed class RegisterVehicleInputValidator : AbstractValidator<RegisterVehicleInput>
{
    public RegisterVehicleInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Vin)
            .NotEmpty().WithMessage("VIN must be exactly 17 uppercase alphanumeric characters (ISO 3779).")
            .Custom((rawVin, ctx) =>
            {
                var check = Iso3779VinValidator.Validate(rawVin);
                if (!check.IsValid)
                {
                    ctx.AddFailure(check.ErrorMessage ?? "Invalid 17-character ISO 3779 VIN.");
                }
            });

        RuleFor(x => x.MotorcycleModelId)
            .NotEmpty().WithMessage("Select a motorcycle model.");

        RuleFor(x => x.CurrentMileageKm)
            .GreaterThanOrEqualTo(0).WithMessage("Current mileage cannot be negative.")
            .LessThanOrEqualTo(MobileValidationPatterns.MaxMileageKm)
            .WithMessage("Current mileage cannot exceed 1,000,000 km.");

        RuleFor(x => x.LicensePlate)
            .Must(p => p!.Trim().Length >= 4)
            .When(x => !string.IsNullOrWhiteSpace(x.LicensePlate))
            .WithMessage("License plate must be at least 4 characters long when supplied.")
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage("License plate must not exceed 20 characters.");

        RuleFor(x => x.ColorName)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.ColorName))
            .WithMessage("Color name must not exceed 60 characters.");
    }
}

public sealed class WarrantyClaimInputValidator : AbstractValidator<WarrantyClaimInput>
{
    public WarrantyClaimInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CustomerVehicleId)
            .NotEmpty().WithMessage("Select a registered motorcycle.");

        RuleFor(x => x.DealerId)
            .NotEmpty().WithMessage("Select a servicing dealership.");

        RuleFor(x => x.OdometerAtClaimKm)
            .GreaterThanOrEqualTo(0).WithMessage("Odometer mileage cannot be negative.")
            .LessThanOrEqualTo(MobileValidationPatterns.MaxMileageKm)
            .WithMessage("Odometer mileage cannot exceed 1,000,000 km.");

        RuleFor(x => x.DefectCategory)
            .NotEmpty().WithMessage("Select a defect category.")
            .Must(cat => MobileValidationPatterns.AllowedDefectCategories.Contains(cat.Trim()))
            .WithMessage("Defect category must be a valid OEM warranty category.");

        RuleFor(x => x.SymptomDescription)
            .NotEmpty().WithMessage("Please describe the defect symptom (at least 10 characters).")
            .Must(s => s.Trim().Length >= MobileValidationPatterns.MinSymptomLength)
            .WithMessage($"Defect symptom description must be at least {MobileValidationPatterns.MinSymptomLength} characters.")
            .Must(s => s.Trim().Length <= MobileValidationPatterns.MaxSymptomLength)
            .WithMessage($"Defect symptom description must not exceed {MobileValidationPatterns.MaxSymptomLength} characters.");
    }
}

public sealed class SalesOrderInputValidator : AbstractValidator<SalesOrderInput>
{
    public SalesOrderInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.DealerInventoryId)
            .NotEmpty().WithMessage("Select an Available VIN unit from your dealership inventory.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Enter the buyer's registered email address.")
            .MaximumLength(256).WithMessage("Buyer email must not exceed 256 characters.")
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage("Enter a valid buyer email address.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Select a payment method.")
            .Must(method => MobileValidationPatterns.AllowedPaymentMethods.Contains(method.Trim()))
            .WithMessage("Payment method must be one of: BankTransfer, Cash, Card, EWallet, Installment.");

        RuleFor(x => x.LicensePlate)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage("License plate must not exceed 20 characters.");

        RuleFor(x => x.VoucherCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.VoucherCode))
            .WithMessage("Voucher code must not exceed 32 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Sale notes must not exceed 500 characters.");
    }
}
