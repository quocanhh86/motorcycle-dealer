using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Features.Dealers;

namespace MotorcycleDealer.Application.Features.TimeSlots;

public static class TimeSlotValidationRules
{
    public const int MinCapacity = 1;
    public const int MaxCapacityLimit = 50;
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 240;
    public const int MaxGenerateRangeDays = 60;
    public const int SqlRowVersionByteLength = RowVersionCodec.ByteLength;

    public static bool IsValidOptionalBase64RowVersion(string? rowVersion) =>
        RowVersionCodec.IsValidOptionalBase64(rowVersion);

    public static byte[]? DecodeOptionalRowVersion(string? rowVersion) =>
        RowVersionCodec.DecodeOptionalOrThrow(rowVersion);
}

public sealed class CreateTimeSlotRequestValidator : AbstractValidator<CreateTimeSlotRequest>
{
    public CreateTimeSlotRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.SlotDate)
            .Must(date => date != default)
            .WithMessage("SlotDate is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .WithMessage("StartTime must be in HH:mm or HH:mm:ss format.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("EndTime is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .WithMessage("EndTime must be in HH:mm or HH:mm:ss format.")
            .Must((req, endRaw) =>
                DealerValidationPatterns.TryParseWorkshopTime(req.StartTime, out var start) &&
                DealerValidationPatterns.TryParseWorkshopTime(endRaw, out var end) &&
                end > start)
            .WithMessage("EndTime must be strictly later than StartTime.");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(TimeSlotValidationRules.MinCapacity, TimeSlotValidationRules.MaxCapacityLimit)
            .WithMessage($"MaxCapacity must be between {TimeSlotValidationRules.MinCapacity} and {TimeSlotValidationRules.MaxCapacityLimit} bays.");
    }
}

public sealed class GenerateTimeSlotsRequestValidator : AbstractValidator<GenerateTimeSlotsRequest>
{
    public GenerateTimeSlotsRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FromDate)
            .Must(date => date != default)
            .WithMessage("FromDate is required.");

        RuleFor(x => x)
            .Must(req =>
            {
                var toDate = req.ResolveEffectiveToDate();
                return toDate != default &&
                       toDate >= req.FromDate &&
                       toDate.DayNumber - req.FromDate.DayNumber <= TimeSlotValidationRules.MaxGenerateRangeDays;
            })
            .WithName("toDate")
            .WithMessage($"ToDate must be on or after FromDate and span at most {TimeSlotValidationRules.MaxGenerateRangeDays} days.");

        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(TimeSlotValidationRules.MinDurationMinutes, TimeSlotValidationRules.MaxDurationMinutes)
            .WithMessage($"SlotDurationMinutes must be between {TimeSlotValidationRules.MinDurationMinutes} and {TimeSlotValidationRules.MaxDurationMinutes} minutes.");

        RuleFor(x => x.ResolveRequestedCapacity())
            .Must(cap => cap is null or (>= TimeSlotValidationRules.MinCapacity and <= TimeSlotValidationRules.MaxCapacityLimit))
            .WithName("maxCapacityPerSlot")
            .WithMessage($"MaxCapacityPerSlot must be between {TimeSlotValidationRules.MinCapacity} and {TimeSlotValidationRules.MaxCapacityLimit} bays.");

        RuleFor(x => x.OpeningTime)
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.OpeningTime))
            .WithMessage("OpeningTime must be in HH:mm or HH:mm:ss format.");

        RuleFor(x => x.ClosingTime)
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ClosingTime))
            .WithMessage("ClosingTime must be in HH:mm or HH:mm:ss format.");

        RuleFor(x => x)
            .Must(req =>
                DealerValidationPatterns.TryParseWorkshopTime(req.OpeningTime, out var open) &&
                DealerValidationPatterns.TryParseWorkshopTime(req.ClosingTime, out var close) &&
                close > open)
            .When(x => !string.IsNullOrWhiteSpace(x.OpeningTime) && !string.IsNullOrWhiteSpace(x.ClosingTime))
            .WithName("closingTime")
            .WithMessage("ClosingTime must be strictly later than OpeningTime.");
    }
}

public sealed class UpdateTimeSlotRequestValidator : AbstractValidator<UpdateTimeSlotRequest>
{
    public UpdateTimeSlotRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x)
            .Must(req => req.MaxCapacity.HasValue || req.IsBlocked.HasValue)
            .WithName("request")
            .WithMessage("At least one of maxCapacity or isBlocked must be provided.");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(TimeSlotValidationRules.MinCapacity, TimeSlotValidationRules.MaxCapacityLimit)
            .When(x => x.MaxCapacity.HasValue)
            .WithMessage($"MaxCapacity must be between {TimeSlotValidationRules.MinCapacity} and {TimeSlotValidationRules.MaxCapacityLimit} bays.");

        RuleFor(x => x.RowVersion)
            .Must(TimeSlotValidationRules.IsValidOptionalBase64RowVersion)
            .WithMessage("RowVersion must be a valid non-empty Base64 concurrency token.");
    }
}
