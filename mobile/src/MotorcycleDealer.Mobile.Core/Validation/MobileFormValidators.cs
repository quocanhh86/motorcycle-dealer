using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Mobile.Core.Domain;
using MotorcycleDealer.Mobile.Core.Localization;

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
            .NotEmpty().WithMessage(_ => AppResources.Val_EmailRequired)
            .MaximumLength(256).WithMessage(_ => AppResources.Val_EmailMaxLength)
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage(_ => AppResources.Val_EmailInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => AppResources.Val_PasswordRequired)
            .MaximumLength(128).WithMessage(_ => AppResources.Val_PasswordMaxLength);
    }
}

public sealed class RegisterInputValidator : AbstractValidator<RegisterInput>
{
    public RegisterInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => AppResources.Val_EmailRequired)
            .MaximumLength(256).WithMessage(_ => AppResources.Val_EmailMaxLength)
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage(_ => AppResources.Val_EmailInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => AppResources.Val_PasswordRequired)
            .MinimumLength(8).WithMessage(_ => AppResources.Val_PasswordMinLength)
            .MaximumLength(128).WithMessage(_ => AppResources.Val_PasswordMaxLength)
            .Must(MobileValidationPatterns.HasRequiredPasswordComplexity)
            .WithMessage(_ => AppResources.Val_PasswordComplexity);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => AppResources.Val_FullNameRequired)
            .Must(name => name.Trim().Length >= 2).WithMessage(_ => AppResources.Val_FullNameMinLength)
            .MaximumLength(150).WithMessage(_ => AppResources.Val_FullNameMaxLength);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(_ => AppResources.Val_PhoneRequired)
            .MaximumLength(20).WithMessage(_ => AppResources.Val_PhoneMaxLength)
            .Must(phone => MobileValidationPatterns.PhoneRegex().IsMatch(phone.Trim()))
            .WithMessage(_ => AppResources.Val_PhoneInvalid);
    }
}

public sealed class RegisterVehicleInputValidator : AbstractValidator<RegisterVehicleInput>
{
    public RegisterVehicleInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Vin)
            .NotEmpty().WithMessage(_ => AppResources.Val_VinRequired)
            .Custom((rawVin, ctx) =>
            {
                var check = Iso3779VinValidator.Validate(rawVin);
                if (!check.IsValid)
                {
                    ctx.AddFailure(check.ErrorMessage ?? AppResources.Val_VinInvalidFallback);
                }
            });

        RuleFor(x => x.MotorcycleModelId)
            .NotEmpty().WithMessage(_ => AppResources.Val_ModelRequired);

        RuleFor(x => x.CurrentMileageKm)
            .GreaterThanOrEqualTo(0).WithMessage(_ => AppResources.Val_MileageNonNegative)
            .LessThanOrEqualTo(MobileValidationPatterns.MaxMileageKm)
            .WithMessage(_ => AppResources.Val_MileageMax);

        RuleFor(x => x.LicensePlate)
            .Must(p => p!.Trim().Length >= 4)
            .When(x => !string.IsNullOrWhiteSpace(x.LicensePlate))
            .WithMessage(_ => AppResources.Val_LicensePlateMinLength)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage(_ => AppResources.Val_LicensePlateMaxLength);

        RuleFor(x => x.ColorName)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.ColorName))
            .WithMessage(_ => AppResources.Val_ColorNameMaxLength);
    }
}

public sealed class WarrantyClaimInputValidator : AbstractValidator<WarrantyClaimInput>
{
    public WarrantyClaimInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CustomerVehicleId)
            .NotEmpty().WithMessage(_ => AppResources.Val_VehicleRequired);

        RuleFor(x => x.DealerId)
            .NotEmpty().WithMessage(_ => AppResources.Val_DealerRequired);

        RuleFor(x => x.OdometerAtClaimKm)
            .GreaterThanOrEqualTo(0).WithMessage(_ => AppResources.Val_ClaimOdometerNonNegative)
            .LessThanOrEqualTo(MobileValidationPatterns.MaxMileageKm)
            .WithMessage(_ => AppResources.Val_ClaimOdometerMax);

        RuleFor(x => x.DefectCategory)
            .NotEmpty().WithMessage(_ => AppResources.Val_DefectCategoryRequired)
            .Must(cat => MobileValidationPatterns.AllowedDefectCategories.Contains(cat.Trim()))
            .WithMessage(_ => AppResources.Val_DefectCategoryInvalid);

        RuleFor(x => x.SymptomDescription)
            .NotEmpty().WithMessage(_ => AppResources.Val_SymptomRequired)
            .Must(s => s.Trim().Length >= MobileValidationPatterns.MinSymptomLength)
            .WithMessage(_ => AppResources.Format("Val_SymptomMinLength", MobileValidationPatterns.MinSymptomLength))
            .Must(s => s.Trim().Length <= MobileValidationPatterns.MaxSymptomLength)
            .WithMessage(_ => AppResources.Format("Val_SymptomMaxLength", MobileValidationPatterns.MaxSymptomLength));
    }
}

public sealed class SalesOrderInputValidator : AbstractValidator<SalesOrderInput>
{
    public SalesOrderInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.DealerInventoryId)
            .NotEmpty().WithMessage(_ => AppResources.Val_InventoryRequired);

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage(_ => AppResources.Val_BuyerEmailRequired)
            .MaximumLength(256).WithMessage(_ => AppResources.Val_BuyerEmailMaxLength)
            .Must(email => MobileValidationPatterns.EmailRegex().IsMatch(email.Trim()))
            .WithMessage(_ => AppResources.Val_BuyerEmailInvalid);

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage(_ => AppResources.Val_PaymentMethodRequired)
            .Must(method => MobileValidationPatterns.AllowedPaymentMethods.Contains(method.Trim()))
            .WithMessage(_ => AppResources.Val_PaymentMethodInvalid);

        RuleFor(x => x.LicensePlate)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage(_ => AppResources.Val_LicensePlateMaxLength);

        RuleFor(x => x.VoucherCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.VoucherCode))
            .WithMessage(_ => AppResources.Val_VoucherCodeMaxLength);

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage(_ => AppResources.Val_SaleNotesMaxLength);
    }
}
