using FluentValidation;
using MotorcycleDealer.Application.Features.TimeSlots;

namespace MotorcycleDealer.Application.Features.Bookings;

public static class BookingValidationRules
{
    public static readonly IReadOnlySet<string> AllowedServiceTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "PeriodicMaintenance",
        "WarrantyRepair",
        "GeneralRepair",
        "Inspection",
        "OilAndTire"
    };

    public static readonly IReadOnlySet<string> AllowedBookingStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Pending",
        "Confirmed",
        "CheckedIn",
        "InProgress",
        "Completed",
        "Cancelled",
        "NoShow"
    };

    public static bool TryNormalizeServiceType(string? raw, out string normalized)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var trimmed = raw.Trim();
            foreach (var candidate in AllowedServiceTypes)
            {
                if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = candidate;
                    return true;
                }
            }
        }

        normalized = string.Empty;
        return false;
    }

    public static bool TryNormalizeBookingStatus(string? raw, out string normalized)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var trimmed = raw.Trim();
            foreach (var candidate in AllowedBookingStatuses)
            {
                if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = candidate;
                    return true;
                }
            }
        }

        normalized = string.Empty;
        return false;
    }
}

public sealed class CreateServiceBookingRequestValidator : AbstractValidator<CreateServiceBookingRequest>
{
    public CreateServiceBookingRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.DealerId)
            .NotEmpty()
            .WithMessage("DealerId is required.");

        RuleFor(x => x.ResolveEffectiveSlotId())
            .NotEmpty()
            .OverridePropertyName("serviceTimeSlotId")
            .WithMessage("ServiceTimeSlotId is required.");

        RuleFor(x => x.CustomerVehicleId)
            .NotEmpty()
            .WithMessage("CustomerVehicleId is required.");

        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("ServiceType is required.")
            .Must(raw => BookingValidationRules.TryNormalizeServiceType(raw, out _))
            .WithMessage("ServiceType must be one of: PeriodicMaintenance, WarrantyRepair, GeneralRepair, Inspection, OilAndTire.");

        RuleFor(x => x.OdometerAtBookingKm)
            .InclusiveBetween(0, 1_000_000)
            .WithMessage("OdometerAtBookingKm must be between 0 and 1,000,000 km.");

        RuleFor(x => x.ReportedNotes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.ReportedNotes))
            .WithMessage("ReportedNotes must not exceed 1,000 characters.");

        RuleFor(x => x.VoucherCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrEmpty(x.VoucherCode))
            .WithMessage("VoucherCode must not exceed 32 characters.");

        RuleFor(x => x.SlotRowVersion)
            .Must(TimeSlotValidationRules.IsValidOptionalBase64RowVersion)
            .WithMessage("SlotRowVersion must be a valid Base64 concurrency token.");

        RuleFor(x => x.VoucherRowVersion)
            .Must(TimeSlotValidationRules.IsValidOptionalBase64RowVersion)
            .WithMessage("VoucherRowVersion must be a valid Base64 concurrency token.");
    }
}

public sealed class UpdateBookingStatusRequestValidator : AbstractValidator<UpdateBookingStatusRequest>
{
    public UpdateBookingStatusRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveStatus())
            .NotEmpty().WithMessage("NewStatus is required.")
            .Must(raw => BookingValidationRules.TryNormalizeBookingStatus(raw, out _))
            .WithMessage("NewStatus must be one of: Pending, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow.")
            .OverridePropertyName("newStatus");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage("CancellationReason must not exceed 300 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 500 characters.");

        RuleFor(x => x.RowVersion)
            .Must(TimeSlotValidationRules.IsValidOptionalBase64RowVersion)
            .WithMessage("RowVersion must be a valid Base64 concurrency token.");
    }
}

public sealed class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CancellationReason)
            .Must(r => r!.Trim().Length >= 3)
            .When(x => !string.IsNullOrWhiteSpace(x.CancellationReason))
            .WithMessage("CancellationReason must be at least 3 characters long.")
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage("CancellationReason must not exceed 300 characters.");

        RuleFor(x => x.Reason)
            .Must(r => r!.Trim().Length >= 3)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason))
            .WithMessage("Reason must be at least 3 characters long.")
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason must not exceed 300 characters.");

        RuleFor(x => x.RowVersion)
            .Must(TimeSlotValidationRules.IsValidOptionalBase64RowVersion)
            .WithMessage("RowVersion must be a valid 8-byte Base64 concurrency token.");
    }
}
