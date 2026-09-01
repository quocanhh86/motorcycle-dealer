using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Application.Features.TestDrives;

public static partial class TestDriveValidationRules
{
    public static readonly TimeOnly OpeningTime = new(8, 0);
    public static readonly TimeOnly ClosingTime = new(18, 0);
    public const int MinDurationMinutes = 30;
    public const int MaxDurationMinutes = 90;
    public const int MaxActiveBookingsPerModelSlot = 2;

    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private static readonly (string Name, TestDriveStatus Value)[] CanonicalStatuses =
    [
        (nameof(TestDriveStatus.Requested), TestDriveStatus.Requested),
        (nameof(TestDriveStatus.Confirmed), TestDriveStatus.Confirmed),
        (nameof(TestDriveStatus.Completed), TestDriveStatus.Completed),
        (nameof(TestDriveStatus.Cancelled), TestDriveStatus.Cancelled),
        (nameof(TestDriveStatus.NoShow), TestDriveStatus.NoShow)
    ];

    [GeneratedRegex(@"^[A-Z0-9\-]{6,20}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex DriverLicenseRegex();

    public static DateOnly GetVietnamToday(DateTimeOffset utcNow) =>
        DateOnly.FromDateTime(utcNow.ToOffset(VietnamUtcOffset).DateTime);

    public static bool TryNormalizeStatus(string? rawStatus, out TestDriveStatus parsedStatus)
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

    public static bool IsValidTimeWindow(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime || startTime < OpeningTime || endTime > ClosingTime)
        {
            return false;
        }

        var durationMinutes = (endTime - startTime).TotalMinutes;
        return durationMinutes >= MinDurationMinutes && durationMinutes <= MaxDurationMinutes;
    }

    public static bool TimesOverlap(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB) =>
        startA < endB && startB < endA;

    public static bool IsAllowedStaffTransition(TestDriveStatus currentStatus, TestDriveStatus nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (TestDriveStatus.Requested, TestDriveStatus.Confirmed or TestDriveStatus.Cancelled) => true,
            (TestDriveStatus.Confirmed, TestDriveStatus.Completed or TestDriveStatus.NoShow or TestDriveStatus.Cancelled) => true,
            _ => false
        };
}

public sealed class CreateTestDriveRequestValidator : AbstractValidator<CreateTestDriveRequest>
{
    public CreateTestDriveRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.DealerId)
            .NotEmpty()
            .WithMessage("DealerId is required.");

        RuleFor(x => x.ResolveEffectiveModelId())
            .NotEmpty()
            .OverridePropertyName("motorcycleModelId")
            .WithMessage("MotorcycleModelId is required.");

        RuleFor(x => x.PreferredDate)
            .Must(d => d != default)
            .WithMessage("PreferredDate is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out var start) &&
                         start >= TestDriveValidationRules.OpeningTime &&
                         start < TestDriveValidationRules.ClosingTime)
            .WithMessage("StartTime must be in HH:mm or HH:mm:ss format within showroom hours (08:00..18:00).");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("EndTime is required.")
            .Must(raw => DealerValidationPatterns.TryParseWorkshopTime(raw, out _))
            .WithMessage("EndTime must be in HH:mm or HH:mm:ss format.")
            .Must((req, endRaw) =>
                DealerValidationPatterns.TryParseWorkshopTime(req.StartTime, out var start) &&
                DealerValidationPatterns.TryParseWorkshopTime(endRaw, out var end) &&
                TestDriveValidationRules.IsValidTimeWindow(start, end))
            .WithMessage("Test-drive window must be within 08:00..18:00, EndTime must be after StartTime, and duration must be between 30 and 90 minutes.");

        RuleFor(x => x.DriverLicenseNumber)
            .NotEmpty().WithMessage("DriverLicenseNumber is required.")
            .MaximumLength(32).WithMessage("DriverLicenseNumber must not exceed 32 characters.")
            .Must(raw => TestDriveValidationRules.DriverLicenseRegex().IsMatch(raw.Trim().ToUpperInvariant()))
            .WithMessage("DriverLicenseNumber must contain 6 to 20 uppercase alphanumeric or hyphen characters.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("ContactPhone is required.")
            .MaximumLength(20).WithMessage("ContactPhone must not exceed 20 characters.")
            .Must(raw => DealerValidationPatterns.BranchPhoneRegex().IsMatch(raw.Trim()))
            .WithMessage("ContactPhone must contain 8 to 15 digits (optional leading '+').");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.CustomerNotes))
            .WithMessage("CustomerNotes must not exceed 500 characters.");
    }
}

public sealed class CancelTestDriveRequestValidator : AbstractValidator<CancelTestDriveRequest>
{
    public CancelTestDriveRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveReason())
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MinimumLength(3).WithMessage("Cancellation reason must be between 3 and 300 characters.")
            .MaximumLength(300).WithMessage("Cancellation reason must be between 3 and 300 characters.")
            .OverridePropertyName("reason");

        RuleFor(x => x.RowVersion)
            .Must(RowVersionCodec.IsValidOptionalBase64)
            .WithMessage("RowVersion must be a valid 8-byte Base64 concurrency token.");
    }
}

public sealed class UpdateTestDriveStatusRequestValidator : AbstractValidator<UpdateTestDriveStatusRequest>
{
    public UpdateTestDriveStatusRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveEffectiveStatus())
            .NotEmpty().WithMessage("Status is required.")
            .Must(raw => TestDriveValidationRules.TryNormalizeStatus(raw, out _))
            .WithMessage("Status must be one of: Requested, Confirmed, Completed, Cancelled, NoShow.")
            .OverridePropertyName("status");

        RuleFor(x => x.StaffNotes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.StaffNotes))
            .WithMessage("StaffNotes must not exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 500 characters.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage("CancellationReason must not exceed 300 characters.");

        RuleFor(x => x.Reason)
            .MaximumLength(300)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason must not exceed 300 characters.");

        RuleFor(x => x.RowVersion)
            .Must(RowVersionCodec.IsValidOptionalBase64)
            .WithMessage("RowVersion must be a valid 8-byte Base64 concurrency token.");
    }
}

public sealed class TestDriveListQueryValidator : AbstractValidator<TestDriveListQuery>
{
    public TestDriveListQueryValidator()
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
                .Must(raw => TestDriveValidationRules.TryNormalizeStatus(raw, out _))
                .OverridePropertyName("status")
                .WithMessage(q => $"Unsupported test-drive status filter '{q.Status}'.");
        });
    }
}
