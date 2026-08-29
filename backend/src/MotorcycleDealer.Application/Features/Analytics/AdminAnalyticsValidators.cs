using FluentValidation;
using FluentValidation.Results;

namespace MotorcycleDealer.Application.Features.Analytics;

public sealed class AnalyticsFilterQueryValidator : AbstractValidator<AnalyticsFilterQuery>
{
    public AnalyticsFilterQueryValidator()
    {
        RuleFor(x => x.DealerId)
            .Must(id => id is null || id.Value != Guid.Empty)
            .WithMessage("dealerId must not be an empty GUID.");

        When(x => x.FromUtc.HasValue && x.ToUtc.HasValue, () =>
        {
            RuleFor(x => x.FromUtc)
                .Must((query, fromUtc) => fromUtc!.Value.ToUniversalTime() <= query.ToUtc!.Value.ToUniversalTime())
                .WithMessage("fromUtc must be less than or equal to toUtc.");

            RuleFor(x => x.ToUtc)
                .Must((query, toUtc) =>
                {
                    var fromNormalized = query.FromUtc!.Value.ToUniversalTime();
                    var toNormalized = toUtc!.Value.ToUniversalTime();
                    return toNormalized < fromNormalized ||
                           (toNormalized - fromNormalized).TotalDays <= AnalyticsConstants.MaxWindowDays;
                })
                .WithMessage($"Analytics date window cannot exceed {AnalyticsConstants.MaxWindowDays} days (24 months).");
        });
    }
}

public static class AdminAnalyticsValidators
{
    private static readonly AnalyticsFilterQueryValidator FilterValidator = new();

    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc, Guid? DealerId) ValidateAndResolveStandardWindow(
        AnalyticsFilterQuery? query,
        DateTimeOffset utcNow)
    {
        var effectiveQuery = query ?? new AnalyticsFilterQuery();
        FilterValidator.ValidateAndThrow(effectiveQuery);

        var normalizedNow = utcNow.ToUniversalTime();
        var effectiveToUtc = (effectiveQuery.ToUtc ?? normalizedNow).ToUniversalTime();
        var effectiveFromUtc = (effectiveQuery.FromUtc ?? effectiveToUtc.AddDays(-AnalyticsConstants.DefaultOverviewWindowDays)).ToUniversalTime();

        EnsureValidResolvedWindow(effectiveFromUtc, effectiveToUtc);
        return (effectiveFromUtc, effectiveToUtc, effectiveQuery.DealerId);
    }

    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc, Guid? DealerId) ValidateAndResolveMonthlyTrendsWindow(
        AnalyticsFilterQuery? query,
        DateTimeOffset utcNow)
    {
        var effectiveQuery = query ?? new AnalyticsFilterQuery();
        FilterValidator.ValidateAndThrow(effectiveQuery);

        var normalizedNow = utcNow.ToUniversalTime();
        var effectiveToUtc = (effectiveQuery.ToUtc ?? normalizedNow).ToUniversalTime();

        DateTimeOffset effectiveFromUtc;
        if (effectiveQuery.FromUtc.HasValue)
        {
            effectiveFromUtc = effectiveQuery.FromUtc.Value.ToUniversalTime();
        }
        else
        {
            var currentMonthStart = new DateTimeOffset(
                effectiveToUtc.Year,
                effectiveToUtc.Month,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

            effectiveFromUtc = currentMonthStart.AddMonths(-(AnalyticsConstants.DefaultMonthlyTrendMonths - 1));
        }

        EnsureValidResolvedWindow(effectiveFromUtc, effectiveToUtc);
        return (effectiveFromUtc, effectiveToUtc, effectiveQuery.DealerId);
    }

    private static void EnsureValidResolvedWindow(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        var failures = new List<ValidationFailure>();

        if (fromUtc > toUtc)
        {
            failures.Add(new ValidationFailure("fromUtc", "fromUtc must be less than or equal to toUtc."));
        }
        else if ((toUtc - fromUtc).TotalDays > AnalyticsConstants.MaxWindowDays)
        {
            failures.Add(new ValidationFailure(
                "toUtc",
                $"Analytics date window cannot exceed {AnalyticsConstants.MaxWindowDays} days (24 months)."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
