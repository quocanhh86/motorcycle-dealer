using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Application.Features.Warranty;

public static class WarrantyClaimValidationRules
{
    public const int MaxWarrantyMonths = WarrantyClaimConstants.MaxWarrantyMonths;
    public const int MaxWarrantyMileageKm = WarrantyClaimConstants.MaxWarrantyMileageKm;

    private static readonly (string Name, WarrantyDefectCategory Value)[] CanonicalCategories =
    [
        (nameof(WarrantyDefectCategory.Engine), WarrantyDefectCategory.Engine),
        (nameof(WarrantyDefectCategory.Transmission), WarrantyDefectCategory.Transmission),
        (nameof(WarrantyDefectCategory.Electrical), WarrantyDefectCategory.Electrical),
        (nameof(WarrantyDefectCategory.BrakesAndSuspension), WarrantyDefectCategory.BrakesAndSuspension),
        (nameof(WarrantyDefectCategory.FrameAndPaint), WarrantyDefectCategory.FrameAndPaint),
        (nameof(WarrantyDefectCategory.ExhaustAndFuel), WarrantyDefectCategory.ExhaustAndFuel)
    ];

    private static readonly (string Name, WarrantyClaimStatus Value)[] CanonicalStatuses =
    [
        (nameof(WarrantyClaimStatus.Submitted), WarrantyClaimStatus.Submitted),
        (nameof(WarrantyClaimStatus.UnderReview), WarrantyClaimStatus.UnderReview),
        (nameof(WarrantyClaimStatus.Approved), WarrantyClaimStatus.Approved),
        (nameof(WarrantyClaimStatus.Completed), WarrantyClaimStatus.Completed),
        (nameof(WarrantyClaimStatus.Rejected), WarrantyClaimStatus.Rejected)
    ];

    public static bool TryNormalizeDefectCategory(string? rawCategory, out WarrantyDefectCategory parsedCategory)
    {
        if (!string.IsNullOrWhiteSpace(rawCategory))
        {
            var trimmed = rawCategory.Trim();
            foreach (var (name, value) in CanonicalCategories)
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    parsedCategory = value;
                    return true;
                }
            }
        }

        parsedCategory = default;
        return false;
    }

    public static bool TryNormalizeStatus(string? rawStatus, out WarrantyClaimStatus parsedStatus)
    {
        if (!string.IsNullOrWhiteSpace(rawStatus))
        {
            var trimmed = rawStatus.Trim();
            foreach (var (name, value) in CanonicalStatuses)
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    parsedStatus = value;
                    return true;
                }
            }
        }

        parsedStatus = default;
        return false;
    }

    public static DateTimeOffset ResolveWarrantyStartUtc(
        DateTimeOffset ownershipStartDateUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? purchasedSalesOrderCompletedAtUtc = null)
    {
        if (ownershipStartDateUtc != default)
        {
            return ownershipStartDateUtc;
        }

        if (purchasedSalesOrderCompletedAtUtc is { } completedAt && completedAt != default)
        {
            return completedAt;
        }

        return createdAtUtc;
    }

    public static DateTimeOffset ComputeWarrantyExpiresAtUtc(DateTimeOffset warrantyStartUtc) =>
        warrantyStartUtc.AddMonths(MaxWarrantyMonths);

    public static bool IsWithinWarrantyCoverage(
        DateTimeOffset warrantyStartUtc,
        DateTimeOffset evaluatedAtUtc,
        int odometerAtClaimKm) =>
        odometerAtClaimKm >= 0 &&
        odometerAtClaimKm <= MaxWarrantyMileageKm &&
        evaluatedAtUtc <= ComputeWarrantyExpiresAtUtc(warrantyStartUtc);

    public static bool IsActiveStatus(WarrantyClaimStatus status) =>
        status is WarrantyClaimStatus.Submitted
            or WarrantyClaimStatus.UnderReview
            or WarrantyClaimStatus.Approved;

    public static bool IsAllowedStatusTransition(WarrantyClaimStatus currentStatus, WarrantyClaimStatus nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (WarrantyClaimStatus.Submitted, WarrantyClaimStatus.UnderReview or WarrantyClaimStatus.Approved or WarrantyClaimStatus.Rejected) => true,
            (WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Approved or WarrantyClaimStatus.Rejected) => true,
            (WarrantyClaimStatus.Approved, WarrantyClaimStatus.Completed) => true,
            _ => false
        };
}

public sealed class CreateWarrantyClaimRequestValidator : AbstractValidator<CreateWarrantyClaimRequest>
{
    public CreateWarrantyClaimRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveVehicleId())
            .NotEmpty()
            .OverridePropertyName("customerVehicleId")
            .WithMessage("CustomerVehicleId is required.");

        RuleFor(x => x.DealerId)
            .NotEmpty()
            .WithMessage("DealerId is required.");

        RuleFor(x => x.OdometerAtClaimKm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("OdometerAtClaimKm must be non-negative.");

        RuleFor(x => x.DefectCategory)
            .NotEmpty()
            .WithMessage("DefectCategory is required.")
            .Must(raw => WarrantyClaimValidationRules.TryNormalizeDefectCategory(raw, out _))
            .WithMessage("DefectCategory must be one of: Engine, Transmission, Electrical, BrakesAndSuspension, FrameAndPaint, ExhaustAndFuel.");

        RuleFor(x => x.SymptomDescription)
            .NotEmpty()
            .WithMessage("SymptomDescription is required.")
            .Must(raw => raw.Trim().Length >= WarrantyClaimConstants.MinSymptomDescriptionLength &&
                         raw.Trim().Length <= WarrantyClaimConstants.MaxSymptomDescriptionLength)
            .WithMessage($"SymptomDescription must be between {WarrantyClaimConstants.MinSymptomDescriptionLength} and {WarrantyClaimConstants.MaxSymptomDescriptionLength} characters.");
    }
}

public sealed class UpdateWarrantyClaimStatusRequestValidator : AbstractValidator<UpdateWarrantyClaimStatusRequest>
{
    public UpdateWarrantyClaimStatusRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveStatus())
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(raw => WarrantyClaimValidationRules.TryNormalizeStatus(raw, out _))
            .WithMessage("Status must be one of: Submitted, UnderReview, Approved, Completed, Rejected.")
            .OverridePropertyName("status");

        RuleFor(x => x.CoveredAmountVnd)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.CoveredAmountVnd.HasValue)
            .WithMessage("CoveredAmountVnd must be non-negative.");

        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(WarrantyClaimConstants.MaxResolutionNotesLength)
            .When(x => !string.IsNullOrEmpty(x.ResolutionNotes))
            .WithMessage($"ResolutionNotes must not exceed {WarrantyClaimConstants.MaxResolutionNotesLength} characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(WarrantyClaimConstants.MaxResolutionNotesLength)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage($"Notes must not exceed {WarrantyClaimConstants.MaxResolutionNotesLength} characters.");

        RuleFor(x => x.RejectionReason)
            .MaximumLength(WarrantyClaimConstants.MaxRejectionReasonLength)
            .When(x => !string.IsNullOrEmpty(x.RejectionReason))
            .WithMessage($"RejectionReason must not exceed {WarrantyClaimConstants.MaxRejectionReasonLength} characters.");

        RuleFor(x => x.Reason)
            .MaximumLength(WarrantyClaimConstants.MaxRejectionReasonLength)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage($"Reason must not exceed {WarrantyClaimConstants.MaxRejectionReasonLength} characters.");

        RuleFor(x => x)
            .Custom((req, context) =>
            {
                if (!WarrantyClaimValidationRules.TryNormalizeStatus(req.ResolveEffectiveStatus(), out var targetStatus))
                {
                    return;
                }

                if (targetStatus == WarrantyClaimStatus.Completed)
                {
                    var resolution = req.ResolveEffectiveResolutionNotes();
                    if (string.IsNullOrWhiteSpace(resolution) ||
                        resolution.Length < WarrantyClaimConstants.MinResolutionNotesLength ||
                        resolution.Length > WarrantyClaimConstants.MaxResolutionNotesLength)
                    {
                        context.AddFailure(
                            "resolutionNotes",
                            $"ResolutionNotes is required and must be between {WarrantyClaimConstants.MinResolutionNotesLength} and {WarrantyClaimConstants.MaxResolutionNotesLength} characters when completing a warranty claim.");
                    }
                }
                else if (targetStatus == WarrantyClaimStatus.Rejected)
                {
                    var rejection = req.ResolveEffectiveRejectionReason();
                    if (string.IsNullOrWhiteSpace(rejection) ||
                        rejection.Length < WarrantyClaimConstants.MinRejectionReasonLength ||
                        rejection.Length > WarrantyClaimConstants.MaxRejectionReasonLength)
                    {
                        context.AddFailure(
                            "rejectionReason",
                            $"RejectionReason is required and must be between {WarrantyClaimConstants.MinRejectionReasonLength} and {WarrantyClaimConstants.MaxRejectionReasonLength} characters when rejecting a warranty claim.");
                    }
                }
            });

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("RowVersion concurrency token is required.")
            .Must(RowVersionCodec.IsValidRequiredBase64)
            .WithMessage("RowVersion must be a valid 8-byte Base64 concurrency token.");
    }
}

public sealed class WarrantyClaimListQueryValidator : AbstractValidator<WarrantyClaimListQuery>
{
    public WarrantyClaimListQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationQuery.MaxPageSize}.");

        When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
        {
            RuleFor(x => x.Status!)
                .Must(raw => WarrantyClaimValidationRules.TryNormalizeStatus(raw, out _))
                .OverridePropertyName("status")
                .WithMessage(q => $"Unsupported warranty claim status filter '{q.Status}'.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.DefectCategory), () =>
        {
            RuleFor(x => x.DefectCategory!)
                .Must(raw => WarrantyClaimValidationRules.TryNormalizeDefectCategory(raw, out _))
                .OverridePropertyName("defectCategory")
                .WithMessage(q => $"Unsupported warranty defect category filter '{q.DefectCategory}'.");
        });
    }
}
