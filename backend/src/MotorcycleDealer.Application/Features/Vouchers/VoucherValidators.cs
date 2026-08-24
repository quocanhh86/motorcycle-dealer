using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;

namespace MotorcycleDealer.Application.Features.Vouchers;

public static partial class VoucherValidationRules
{
    private static readonly string[] CanonicalScopes = ["ServiceOnly", "ShowroomOnly", "All"];
    private static readonly string[] CanonicalDiscountTypes = ["FixedAmount", "Percentage"];
    private static readonly string[] CanonicalVoucherStatuses = ["Available", "ReservedOnBooking", "Used", "Expired"];
    private static readonly string[] CanonicalTierCodes = ["Bronze", "Silver", "Gold", "Platinum"];

    [GeneratedRegex("^[A-Za-z0-9_-]{3,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePrefixRegex();

    public static bool IsValidCodePrefix(string? rawPrefix) =>
        !string.IsNullOrWhiteSpace(rawPrefix) && CodePrefixRegex().IsMatch(rawPrefix.Trim());

    public static bool TryNormalizeApplicableScope(string? rawScope, out string normalizedScope) =>
        TryMatchCanonical(rawScope, CanonicalScopes, out normalizedScope);

    public static bool TryNormalizeDiscountType(string? rawDiscountType, out string normalizedDiscountType) =>
        TryMatchCanonical(rawDiscountType, CanonicalDiscountTypes, out normalizedDiscountType);

    public static bool TryNormalizeVoucherStatus(string? rawStatus, out string normalizedStatus) =>
        TryMatchCanonical(rawStatus, CanonicalVoucherStatuses, out normalizedStatus);

    public static bool TryNormalizeTierCode(string? rawTierCode, out string normalizedTierCode) =>
        TryMatchCanonical(rawTierCode, CanonicalTierCodes, out normalizedTierCode);

    private static bool TryMatchCanonical(string? rawValue, string[] allowed, out string normalizedValue)
    {
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            var trimmed = rawValue.Trim();
            foreach (var candidate in allowed)
            {
                if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedValue = candidate;
                    return true;
                }
            }
        }

        normalizedValue = string.Empty;
        return false;
    }
}

public sealed class VoucherCatalogQueryValidator : AbstractValidator<VoucherCatalogQuery>
{
    public VoucherCatalogQueryValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.ApplicableScope), () =>
        {
            RuleFor(x => x.ApplicableScope)
                .Must(raw => VoucherValidationRules.TryNormalizeApplicableScope(raw, out _))
                .WithMessage("ApplicableScope must be one of ServiceOnly, ShowroomOnly, or All.");
        });
    }
}

public sealed class UpsertVoucherCatalogRequestValidator : AbstractValidator<UpsertVoucherCatalogRequest>
{
    public UpsertVoucherCatalogRequestValidator()
    {
        RuleFor(x => x.CodePrefix)
            .NotEmpty().WithMessage("CodePrefix is required.")
            .Must(VoucherValidationRules.IsValidCodePrefix)
            .WithMessage("CodePrefix must be 3 to 15 alphanumeric, hyphen, or underscore characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        });

        RuleFor(x => x.ApplicableScope)
            .NotEmpty().WithMessage("ApplicableScope is required.")
            .Must(raw => VoucherValidationRules.TryNormalizeApplicableScope(raw, out _))
            .WithMessage("ApplicableScope must be one of ServiceOnly, ShowroomOnly, or All.");

        RuleFor(x => x.PointsCost)
            .GreaterThan(0).WithMessage("PointsCost must be strictly greater than zero.");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("DiscountType is required.")
            .Must(raw => VoucherValidationRules.TryNormalizeDiscountType(raw, out _))
            .WithMessage("DiscountType must be FixedAmount or Percentage.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0m).WithMessage("DiscountValue must be strictly greater than zero.");

        When(x => VoucherValidationRules.TryNormalizeDiscountType(x.DiscountType, out var dt) &&
                  string.Equals(dt, "Percentage", StringComparison.Ordinal), () =>
        {
            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100m)
                .WithMessage("Percentage DiscountValue cannot exceed 100.");

            RuleFor(x => x.MaxDiscountAmount)
                .NotNull()
                .WithMessage("MaxDiscountAmount is required when DiscountType is Percentage.")
                .Must(v => v.HasValue && v.Value > 0m)
                .WithMessage("MaxDiscountAmount must be strictly greater than zero when DiscountType is Percentage.");
        });

        When(x => VoucherValidationRules.TryNormalizeDiscountType(x.DiscountType, out var dt) &&
                  string.Equals(dt, "FixedAmount", StringComparison.Ordinal) &&
                  x.MaxDiscountAmount.HasValue, () =>
        {
            RuleFor(x => x.MaxDiscountAmount!.Value)
                .GreaterThanOrEqualTo(0m)
                .WithMessage("MaxDiscountAmount cannot be negative.");
        });

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0m).WithMessage("MinOrderAmount cannot be negative.");

        RuleFor(x => x.ValidDays)
            .InclusiveBetween(1, 3650).WithMessage("ValidDays must be between 1 and 3650 days.");

        When(x => x.QuotaTotal.HasValue, () =>
        {
            RuleFor(x => x.QuotaTotal!.Value)
                .GreaterThanOrEqualTo(1).WithMessage("QuotaTotal must be at least 1 when specified.");
        });

        RuleFor(x => x)
            .Must(HasValidTierReference)
            .WithName("minTierId")
            .WithMessage("Either a valid minTierId or minTierCode (Bronze, Silver, Gold, Platinum) must be provided.");
    }

    private static bool HasValidTierReference(UpsertVoucherCatalogRequest request)
    {
        if (request.MinTierId is { } tierId && tierId != Guid.Empty)
        {
            return true;
        }

        return VoucherValidationRules.TryNormalizeTierCode(request.MinTierCode, out _);
    }
}

public sealed class MyCustomerVouchersQueryValidator : AbstractValidator<MyCustomerVouchersQuery>
{
    public MyCustomerVouchersQueryValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(raw => VoucherValidationRules.TryNormalizeVoucherStatus(raw, out _))
                .WithMessage("Status must be one of Available, ReservedOnBooking, Used, or Expired.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ApplicableScope), () =>
        {
            RuleFor(x => x.ApplicableScope)
                .Must(raw => VoucherValidationRules.TryNormalizeApplicableScope(raw, out _))
                .WithMessage("ApplicableScope must be one of ServiceOnly, ShowroomOnly, or All.");
        });
    }
}

public sealed class RedeemVoucherRequestValidator : AbstractValidator<RedeemVoucherRequest>
{
    public RedeemVoucherRequestValidator()
    {
        RuleFor(x => x.ResolveEffectiveCatalogId())
            .NotEmpty()
            .WithName("voucherCatalogId")
            .WithMessage("VoucherCatalogId is required.");

        RuleFor(x => x.ResolveEffectiveRowVersion())
            .Must(RowVersionCodec.IsValidOptionalBase64)
            .WithName("accountRowVersion")
            .WithMessage("AccountRowVersion must be a valid Base64-encoded 8-byte concurrency token when supplied.");

        When(x => x.IdempotencyKey is not null, () =>
        {
            RuleFor(x => x.IdempotencyKey!)
                .Must(k => !string.IsNullOrWhiteSpace(k) && k.Trim().Length <= 120)
                .WithMessage("IdempotencyKey must be between 1 and 120 non-whitespace characters when supplied.");
        });
    }
}
