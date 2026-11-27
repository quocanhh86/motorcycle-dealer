using FluentValidation;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Showroom;

namespace MotorcycleDealer.Application.Features.SalesOrders;

public static class SalesOrderValidationRules
{
    public static readonly IReadOnlySet<string> AllowedPaymentMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cash",
        "BankTransfer",
        "Card",
        "Installment"
    };

    public static bool TryNormalizePaymentMethod(string? rawMethod, out string normalizedMethod)
    {
        if (string.IsNullOrWhiteSpace(rawMethod))
        {
            normalizedMethod = "BankTransfer";
            return true;
        }

        var trimmed = rawMethod.Trim();
        foreach (var canonical in AllowedPaymentMethods)
        {
            if (string.Equals(canonical, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalizedMethod = canonical;
                return true;
            }
        }

        normalizedMethod = string.Empty;
        return false;
    }
}

public sealed class CreateSalesOrderRequestValidator : AbstractValidator<CreateSalesOrderRequest>
{
    public CreateSalesOrderRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.DealerInventoryId)
            .NotEmpty()
            .WithMessage("DealerInventoryId is required.");

        RuleFor(x => x)
            .Must(req =>
                (req.CustomerId.HasValue && req.CustomerId.Value != Guid.Empty) ||
                !string.IsNullOrWhiteSpace(req.CustomerEmail))
            .WithName("customerId")
            .WithMessage("Either a valid customerId or customerEmail must be provided.");

        RuleFor(x => x.CustomerEmail)
            .MaximumLength(256).WithMessage("CustomerEmail must not exceed 256 characters.")
            .Must(email => AuthValidationPatterns.EmailRegex().IsMatch(email!.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail))
            .WithMessage("CustomerEmail must be a valid email address.");

        RuleFor(x => x.ResolveEffectiveRowVersion())
            .Must(ShowroomValidationRules.IsValidRowVersionBase64)
            .When(x => x.ResolveEffectiveRowVersion() is not null)
            .OverridePropertyName("inventoryRowVersion")
            .WithMessage("InventoryRowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");

        RuleFor(x => x.CustomerVoucherRowVersion)
            .Must(ShowroomValidationRules.IsValidRowVersionBase64)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerVoucherRowVersion))
            .WithMessage("CustomerVoucherRowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.DiscountAmount.HasValue)
            .WithMessage("DiscountAmount cannot be negative.");

        RuleFor(x => x.PaymentMethod)
            .Must(method => SalesOrderValidationRules.TryNormalizePaymentMethod(method, out _))
            .WithMessage("PaymentMethod must be one of: Cash, BankTransfer, Card, Installment.");

        RuleFor(x => x.LicensePlate)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage("LicensePlate must not exceed 20 characters.");

        RuleFor(x => x.VoucherCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.VoucherCode))
            .WithMessage("VoucherCode must not exceed 32 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 500 characters.");
    }
}
