using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Application.Common.Concurrency;

namespace MotorcycleDealer.Application.Features.Showroom;

public static partial class ShowroomValidationRules
{
    public static readonly IReadOnlySet<string> AllowedCategories = new HashSet<string>(StringComparer.Ordinal)
    {
        "Scooter",
        "Underbone",
        "Sport",
        "Naked",
        "Adventure",
        "Cruiser"
    };

    [GeneratedRegex(@"^[A-Z0-9_-]{3,30}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex CodeRegex();

    public static bool TryNormalizeCategory(string? rawCategory, out string normalizedCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
        {
            normalizedCategory = string.Empty;
            return false;
        }

        var trimmed = rawCategory.Trim();
        foreach (var canonical in AllowedCategories)
        {
            if (string.Equals(canonical, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalizedCategory = canonical;
                return true;
            }
        }

        normalizedCategory = string.Empty;
        return false;
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
            return doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool IsValidRowVersionBase64(string? rawToken) =>
        RowVersionCodec.IsValidRequiredBase64(rawToken);
}

public sealed class UpsertMotorcycleModelRequestValidator : AbstractValidator<UpsertMotorcycleModelRequest>
{
    public UpsertMotorcycleModelRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ModelCode)
            .NotEmpty().WithMessage("Model code is required.")
            .MaximumLength(30).WithMessage("Model code must not exceed 30 characters.")
            .Must(c => ShowroomValidationRules.CodeRegex().IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Model code must contain 3 to 30 uppercase alphanumeric, hyphen, or underscore characters.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .Must(b => b.Trim().Length >= 2).WithMessage("Brand must be at least 2 characters long.")
            .MaximumLength(80).WithMessage("Brand must not exceed 80 characters.");

        RuleFor(x => x.ModelName)
            .NotEmpty().WithMessage("Model name is required.")
            .Must(m => m.Trim().Length >= 2).WithMessage("Model name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Model name must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Motorcycle category is required.")
            .Must(cat => ShowroomValidationRules.TryNormalizeCategory(cat, out _))
            .WithMessage("Category must be one of: Scooter, Underbone, Sport, Naked, Adventure, Cruiser.");

        RuleFor(x => x.EngineDisplacementCc)
            .InclusiveBetween(49, 2500)
            .WithMessage("Engine displacement (cc) must be between 49 and 2500.");

        RuleFor(x => x.BaseRetailPrice)
            .GreaterThan(0m)
            .WithMessage("Base retail price must be strictly greater than 0 VND.");

        RuleFor(x => x.WarrantyMonths)
            .GreaterThan(0)
            .LessThanOrEqualTo(120)
            .WithMessage("Warranty months must be between 1 and 120.");

        RuleFor(x => x.WarrantyMileageKm)
            .GreaterThan(0)
            .LessThanOrEqualTo(500_000)
            .WithMessage("Warranty mileage (km) must be between 1 and 500,000.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Image URL must not exceed 500 characters.");

        RuleFor(x => x.SpecificationsJson)
            .Must(ShowroomValidationRules.IsValidOptionalJson)
            .WithMessage("SpecificationsJson must be null, empty, or a valid JSON object/array.");
    }
}

public sealed class UpsertMaintenanceRuleRequestValidator : AbstractValidator<UpsertMaintenanceRuleRequest>
{
    public UpsertMaintenanceRuleRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.MilestoneCode)
            .NotEmpty().WithMessage("Milestone code is required.")
            .MaximumLength(30).WithMessage("Milestone code must not exceed 30 characters.")
            .Must(c => ShowroomValidationRules.CodeRegex().IsMatch(c.Trim().ToUpperInvariant()))
            .WithMessage("Milestone code must contain 3 to 30 uppercase alphanumeric, hyphen, or underscore characters.");

        RuleFor(x => x.MilestoneName)
            .NotEmpty().WithMessage("Milestone name is required.")
            .Must(n => n.Trim().Length >= 2).WithMessage("Milestone name must be at least 2 characters long.")
            .MaximumLength(120).WithMessage("Milestone name must not exceed 120 characters.");

        RuleFor(x => x.IntervalKm)
            .GreaterThan(0)
            .LessThanOrEqualTo(200_000)
            .WithMessage("IntervalKm must be strictly greater than 0 km.");

        RuleFor(x => x.IntervalMonths)
            .GreaterThan(0)
            .LessThanOrEqualTo(120)
            .WithMessage("IntervalMonths must be strictly greater than 0 months.");

        RuleFor(x => x.LeadTimeDays)
            .InclusiveBetween(0, 90)
            .WithMessage("LeadTimeDays must be between 0 and 90 days.");

        RuleFor(x => x.LeadMileageKm)
            .InclusiveBetween(0, 5_000)
            .WithMessage("LeadMileageKm must be between 0 and 5,000 km.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .InclusiveBetween(15, 720)
            .WithMessage("EstimatedDurationMinutes must be between 15 and 720 minutes.");

        RuleFor(x => x.ChecklistSummary)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.ChecklistSummary))
            .WithMessage("ChecklistSummary must not exceed 1,000 characters.");
    }
}
