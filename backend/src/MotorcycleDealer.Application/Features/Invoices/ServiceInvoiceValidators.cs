using System.Text.Json;
using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;

namespace MotorcycleDealer.Application.Features.Invoices;

public static class ServiceInvoiceValidationRules
{
    public static readonly IReadOnlySet<string> AllowedPaymentMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cash",
        "BankTransfer",
        "Card",
        "EWallet"
    };

    public static readonly IReadOnlySet<string> AllowedLineItemTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Part",
        "Labor"
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

    public static bool TryNormalizeLineItemType(string? rawItemType, out string normalizedItemType)
    {
        if (string.IsNullOrWhiteSpace(rawItemType))
        {
            normalizedItemType = "Part";
            return true;
        }

        var trimmed = rawItemType.Trim();
        foreach (var canonical in AllowedLineItemTypes)
        {
            if (string.Equals(canonical, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalizedItemType = canonical;
                return true;
            }
        }

        normalizedItemType = string.Empty;
        return false;
    }

    public static decimal ComputeLineTotalVnd(int quantity, decimal unitPriceVnd)
    {
        var roundedUnitPrice = Math.Round(unitPriceVnd, 2, MidpointRounding.AwayFromZero);
        return Math.Round(quantity * roundedUnitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsValidOptionalJson(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class CreateServiceInvoiceLineItemRequestValidator : AbstractValidator<CreateServiceInvoiceLineItemRequest>
{
    public CreateServiceInvoiceLineItemRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ItemType)
            .Must(raw => ServiceInvoiceValidationRules.TryNormalizeLineItemType(raw, out _))
            .WithMessage("LineItem ItemType must be 'Part' or 'Labor'.");

        RuleFor(x => x)
            .Must(item =>
            {
                if (item.ResolveEffectiveSparePartId() is null)
                {
                    return true;
                }

                return ServiceInvoiceValidationRules.TryNormalizeLineItemType(item.ItemType, out var normalized) &&
                       string.Equals(normalized, "Part", StringComparison.Ordinal);
            })
            .WithName("itemType")
            .WithMessage("Line items referencing a SparePartId must use ItemType 'Part'.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 1_000)
            .WithMessage("LineItem Quantity must be between 1 and 1,000.");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("LineItem Description must not exceed 200 characters.");

        RuleFor(x => x)
            .Must(item => item.ResolveEffectiveSparePartId() is not null ||
                          !string.IsNullOrWhiteSpace(item.Description))
            .WithName("description")
            .WithMessage("Description is required when SparePartId is not specified.");

        RuleFor(x => x.UnitPriceVnd)
            .GreaterThan(0m)
            .When(x => x.UnitPriceVnd.HasValue)
            .WithMessage("LineItem UnitPriceVnd must be strictly greater than 0 VND when provided.");

        RuleFor(x => x)
            .Must(item => item.ResolveEffectiveSparePartId() is not null ||
                          (item.UnitPriceVnd is > 0m))
            .WithName("unitPriceVnd")
            .WithMessage("UnitPriceVnd (> 0) is required when SparePartId is not specified.");
    }
}

public sealed class CreateServiceInvoiceRequestValidator : AbstractValidator<CreateServiceInvoiceRequest>
{
    public CreateServiceInvoiceRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveBookingId())
            .NotEmpty()
            .OverridePropertyName("serviceBookingId")
            .WithMessage("ServiceBookingId is required.");

        RuleFor(x => x.LaborAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("LaborAmount cannot be negative.");

        RuleFor(x => x.PartsAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("PartsAmount cannot be negative.");

        RuleFor(x => x.LineItems)
            .Must(items => items is null || items.Count <= 50)
            .WithMessage("LineItems must not exceed 50 rows per service invoice.");

        RuleForEach(x => x.LineItems!)
            .SetValidator(new CreateServiceInvoiceLineItemRequestValidator())
            .When(x => x.LineItems is { Count: > 0 });

        RuleFor(x => x)
            .Must(req =>
                req.LineItems is { Count: > 0 } ||
                (req.LaborAmount >= 0m && req.PartsAmount >= 0m && (req.LaborAmount + req.PartsAmount) > 0m))
            .OverridePropertyName("grossAmount")
            .WithMessage("GrossAmount (LaborAmount + PartsAmount or itemized LineItems) must be greater than zero.");

        RuleFor(x => x.OdometerAtCompletionKm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("OdometerAtCompletionKm must be greater than or equal to 0.");

        RuleFor(x => x.ResolveEffectiveBookingRowVersion())
            .Must(RowVersionCodec.IsValidRequiredBase64)
            .When(x => x.ResolveEffectiveBookingRowVersion() is not null)
            .OverridePropertyName("bookingRowVersion")
            .WithMessage("BookingRowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");

        RuleFor(x => x.CustomerVoucherRowVersion)
            .Must(RowVersionCodec.IsValidRequiredBase64)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerVoucherRowVersion))
            .WithMessage("CustomerVoucherRowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");

        RuleFor(x => x.PaymentMethod)
            .Must(method => ServiceInvoiceValidationRules.TryNormalizePaymentMethod(method, out _))
            .WithMessage("PaymentMethod must be one of: Cash, BankTransfer, Card, EWallet.");

        RuleFor(x => x.VoucherCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.VoucherCode))
            .WithMessage("VoucherCode must not exceed 32 characters.");

        RuleFor(x => x.WorkSummaryNotes)
            .MaximumLength(1500)
            .When(x => !string.IsNullOrEmpty(x.WorkSummaryNotes))
            .WithMessage("WorkSummaryNotes must not exceed 1500 characters.");

        RuleFor(x => x.LineItemsJson)
            .Must(ServiceInvoiceValidationRules.IsValidOptionalJson)
            .When(x => !string.IsNullOrWhiteSpace(x.LineItemsJson))
            .WithMessage("LineItemsJson must be a valid JSON object or array when provided.");
    }
}
