using System.Text.Json;
using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Application.Features.Loyalty;

public static class LoyaltyValidationRules
{
    private static readonly string[] CanonicalTransactionTypes =
    [
        nameof(LedgerTransactionType.SalesOrderEarn),
        nameof(LedgerTransactionType.ServiceInvoiceEarn),
        nameof(LedgerTransactionType.VoucherRedeem),
        nameof(LedgerTransactionType.VoucherExpireRefund),
        nameof(LedgerTransactionType.AdminAdjustment),
        nameof(LedgerTransactionType.WelcomeBonus),
        nameof(LedgerTransactionType.ServiceReviewBonus)
    ];

    public static bool TryNormalizeTransactionType(string? rawType, out string normalizedType)
    {
        if (!string.IsNullOrWhiteSpace(rawType))
        {
            var trimmed = rawType.Trim();
            foreach (var candidate in CanonicalTransactionTypes)
            {
                if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedType = candidate;
                    return true;
                }
            }
        }

        normalizedType = string.Empty;
        return false;
    }

    public static bool IsValidJsonPayload(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return false;
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

public sealed class UpdateLoyaltyTierRequestValidator : AbstractValidator<UpdateLoyaltyTierRequest>
{
    public UpdateLoyaltyTierRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(60).WithMessage("DisplayName cannot exceed 60 characters.");

        When(x => x.PointsMultiplier.HasValue, () =>
        {
            RuleFor(x => x.PointsMultiplier!.Value)
                .GreaterThanOrEqualTo(1.00m).WithMessage("PointsMultiplier must be at least 1.00.")
                .LessThanOrEqualTo(10.00m).WithMessage("PointsMultiplier cannot exceed 10.00.");
        });

        When(x => x.BenefitsJson is not null, () =>
        {
            RuleFor(x => x.BenefitsJson)
                .Must(LoyaltyValidationRules.IsValidJsonPayload)
                .WithMessage("BenefitsJson must be a valid JSON array or object.");
        });

        RuleFor(x => x.RowVersion)
            .Must(RowVersionCodec.IsValidOptionalBase64)
            .WithMessage("RowVersion must be a valid Base64-encoded 8-byte concurrency token when supplied.");
    }
}

public sealed class PointLedgerQueryValidator : AbstractValidator<PointLedgerQuery>
{
    public PointLedgerQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationQuery.MaxPageSize}.");

        When(x => !string.IsNullOrWhiteSpace(x.TransactionType), () =>
        {
            RuleFor(x => x.TransactionType)
                .Must(raw => LoyaltyValidationRules.TryNormalizeTransactionType(raw, out _))
                .WithMessage("TransactionType must be one of SalesOrderEarn, ServiceInvoiceEarn, VoucherRedeem, VoucherExpireRefund, AdminAdjustment, WelcomeBonus, or ServiceReviewBonus.");
        });
    }
}

public sealed class AdminPointAdjustmentRequestValidator : AbstractValidator<AdminPointAdjustmentRequest>
{
    public AdminPointAdjustmentRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasTargetAccountOrCustomer)
            .WithName("customerId")
            .WithMessage("Either customerId, loyaltyAccountId, or customerEmail must be provided.");

        RuleFor(x => x.PointsDelta)
            .NotEqual(0).WithMessage("PointsDelta must be non-zero.");

        RuleFor(x => x.ResolveEffectiveNotes())
            .NotEmpty()
            .WithName("notes")
            .WithMessage("Audit notes are required for manual point adjustments.")
            .MinimumLength(5)
            .WithName("notes")
            .WithMessage("Audit notes must be at least 5 characters long.")
            .MaximumLength(500)
            .WithName("notes")
            .WithMessage("Audit notes cannot exceed 500 characters.");

        When(x => x.IdempotencyKey is not null, () =>
        {
            RuleFor(x => x.IdempotencyKey!)
                .Must(k => !string.IsNullOrWhiteSpace(k) && k.Trim().Length <= 120)
                .WithMessage("IdempotencyKey must be between 1 and 120 non-whitespace characters when supplied.");
        });

        RuleFor(x => x.ResolveEffectiveRowVersion())
            .Must(RowVersionCodec.IsValidOptionalBase64)
            .WithName("rowVersion")
            .WithMessage("RowVersion must be a valid Base64-encoded 8-byte concurrency token when supplied.");
    }

    private static bool HasTargetAccountOrCustomer(AdminPointAdjustmentRequest request) =>
        request.ResolveTargetAccountId().HasValue ||
        (request.CustomerId is { } customerId && customerId != Guid.Empty) ||
        !string.IsNullOrWhiteSpace(request.CustomerEmail);
}
