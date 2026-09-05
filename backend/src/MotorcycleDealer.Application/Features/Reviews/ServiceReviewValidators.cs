using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Reviews;

public static class ServiceReviewValidationRules
{
    public const int StandardReviewBonusPoints = ServiceReviewConstants.StandardReviewBonusPoints;
    private static readonly char[] TagDelimiters = [',', ';', '|'];

    public static IReadOnlyList<string> CanonicalQualityTags { get; } =
    [
        "FastTurnaround",
        "TransparentPricing",
        "ExpertTechnician",
        "CleanLounge",
        "GenuineParts",
        "FriendlyAdvisor",
        "ClearExplanation",
        "ThoroughInspection"
    ];

    public static bool TryNormalizeQualityTags(
        string? rawTags,
        out string? normalizedCsv,
        out IReadOnlyList<string> normalizedList,
        out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            normalizedCsv = null;
            normalizedList = [];
            errorMessage = null;
            return true;
        }

        var rawSegments = rawTags.Split(TagDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rawSegments.Length == 0)
        {
            normalizedCsv = null;
            normalizedList = [];
            errorMessage = null;
            return true;
        }

        if (rawSegments.Length > ServiceReviewConstants.MaxQualityTagsCount)
        {
            normalizedCsv = null;
            normalizedList = [];
            errorMessage = $"QualityTags may include at most {ServiceReviewConstants.MaxQualityTagsCount} tags.";
            return false;
        }

        var canonicalResult = new List<string>(rawSegments.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawSegment in rawSegments)
        {
            var matched = CanonicalQualityTags.FirstOrDefault(c =>
                string.Equals(c, rawSegment, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                normalizedCsv = null;
                normalizedList = [];
                errorMessage =
                    $"Unsupported quality tag '{rawSegment}'. Allowed tags: {string.Join(", ", CanonicalQualityTags)}.";
                return false;
            }

            if (seen.Add(matched))
            {
                canonicalResult.Add(matched);
            }
        }

        var joined = string.Join(',', canonicalResult);
        if (joined.Length > ServiceReviewConstants.MaxQualityTagsLength)
        {
            normalizedCsv = null;
            normalizedList = [];
            errorMessage = $"QualityTags must not exceed {ServiceReviewConstants.MaxQualityTagsLength} characters.";
            return false;
        }

        normalizedCsv = joined;
        normalizedList = canonicalResult;
        errorMessage = null;
        return true;
    }

    public static IReadOnlyList<string> SplitStoredQualityTags(string? storedCsv)
    {
        if (string.IsNullOrWhiteSpace(storedCsv))
        {
            return [];
        }

        return storedCsv
            .Split(TagDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}

public sealed class CreateServiceReviewRequestValidator : AbstractValidator<CreateServiceReviewRequest>
{
    public CreateServiceReviewRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveInvoiceId())
            .NotEmpty()
            .OverridePropertyName("serviceInvoiceId")
            .WithMessage("ServiceInvoiceId is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(ServiceReviewConstants.MinRating, ServiceReviewConstants.MaxRating)
            .WithMessage($"Rating must be between {ServiceReviewConstants.MinRating} and {ServiceReviewConstants.MaxRating} stars.");

        RuleFor(x => x)
            .Custom((req, context) =>
            {
                var rawTags = req.ResolveRawQualityTags();
                if (rawTags is not null && rawTags.Length > ServiceReviewConstants.MaxQualityTagsLength)
                {
                    context.AddFailure(
                        "qualityTags",
                        $"QualityTags must not exceed {ServiceReviewConstants.MaxQualityTagsLength} characters.");
                    return;
                }

                if (!ServiceReviewValidationRules.TryNormalizeQualityTags(rawTags, out _, out _, out var error))
                {
                    context.AddFailure("qualityTags", error!);
                }
            });

        RuleFor(x => x.Comment)
            .Must(comment => comment!.Trim().Length <= ServiceReviewConstants.MaxCommentLength)
            .When(x => !string.IsNullOrEmpty(x.Comment))
            .WithMessage($"Comment must not exceed {ServiceReviewConstants.MaxCommentLength} characters.");
    }
}

public sealed class RespondToServiceReviewRequestValidator : AbstractValidator<RespondToServiceReviewRequest>
{
    public RespondToServiceReviewRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveStaffResponse())
            .NotEmpty()
            .WithMessage("StaffResponse is required.")
            .MinimumLength(ServiceReviewConstants.MinStaffResponseLength)
            .WithMessage($"StaffResponse must be between {ServiceReviewConstants.MinStaffResponseLength} and {ServiceReviewConstants.MaxStaffResponseLength} characters.")
            .MaximumLength(ServiceReviewConstants.MaxStaffResponseLength)
            .WithMessage($"StaffResponse must be between {ServiceReviewConstants.MinStaffResponseLength} and {ServiceReviewConstants.MaxStaffResponseLength} characters.")
            .OverridePropertyName("staffResponse");

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("RowVersion concurrency token is required.")
            .Must(RowVersionCodec.IsValidRequiredBase64)
            .WithMessage("RowVersion must be a valid 8-byte Base64 concurrency token.");
    }
}

public sealed class ServiceReviewListQueryValidator : AbstractValidator<ServiceReviewListQuery>
{
    public ServiceReviewListQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationQuery.MaxPageSize}.");

        When(x => x.Rating.HasValue, () =>
        {
            RuleFor(x => x.Rating!.Value)
                .InclusiveBetween(ServiceReviewConstants.MinRating, ServiceReviewConstants.MaxRating)
                .WithName("rating")
                .WithMessage($"Rating filter must be between {ServiceReviewConstants.MinRating} and {ServiceReviewConstants.MaxRating}.");
        });

        When(x => x.MinRating.HasValue, () =>
        {
            RuleFor(x => x.MinRating!.Value)
                .InclusiveBetween(ServiceReviewConstants.MinRating, ServiceReviewConstants.MaxRating)
                .WithName("minRating")
                .WithMessage($"MinRating filter must be between {ServiceReviewConstants.MinRating} and {ServiceReviewConstants.MaxRating}.");
        });
    }
}
