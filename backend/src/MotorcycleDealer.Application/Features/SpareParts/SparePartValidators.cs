using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Application.Features.SpareParts;

public static partial class SparePartValidationRules
{
    private static readonly (string Name, SparePartCategory Value)[] CanonicalCategories =
    [
        (nameof(SparePartCategory.EngineOil), SparePartCategory.EngineOil),
        (nameof(SparePartCategory.Filter), SparePartCategory.Filter),
        (nameof(SparePartCategory.Brakes), SparePartCategory.Brakes),
        (nameof(SparePartCategory.Transmission), SparePartCategory.Transmission),
        (nameof(SparePartCategory.Tires), SparePartCategory.Tires),
        (nameof(SparePartCategory.Electrical), SparePartCategory.Electrical),
        (nameof(SparePartCategory.Accessory), SparePartCategory.Accessory)
    ];

    [GeneratedRegex(@"^[A-Z0-9_-]{3,32}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex PartCodeRegex();

    public static bool TryNormalizeCategory(string? rawCategory, out SparePartCategory parsedCategory)
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
}

public sealed class UpsertSparePartRequestValidator : AbstractValidator<UpsertSparePartRequest>
{
    public UpsertSparePartRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.PartCode)
            .NotEmpty().WithMessage("PartCode is required.")
            .MaximumLength(32).WithMessage("PartCode must not exceed 32 characters.")
            .Must(code => SparePartValidationRules.PartCodeRegex().IsMatch(code.Trim().ToUpperInvariant()))
            .WithMessage("PartCode must contain 3 to 32 uppercase alphanumeric, hyphen, or underscore characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required.")
            .Must(n => n.Trim().Length >= 2).WithMessage("Part name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Part name must not exceed 150 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(raw => SparePartValidationRules.TryNormalizeCategory(raw, out _))
            .WithMessage("Category must be one of: EngineOil, Filter, Brakes, Transmission, Tires, Electrical, Accessory.");

        RuleFor(x => x.UnitLabel)
            .NotEmpty().WithMessage("UnitLabel is required.")
            .Must(u => u.Trim().Length >= 1).WithMessage("UnitLabel is required.")
            .MaximumLength(24).WithMessage("UnitLabel must not exceed 24 characters.");

        RuleFor(x => x.UnitPriceVnd)
            .GreaterThan(0m)
            .WithMessage("UnitPriceVnd must be strictly greater than 0 VND.");
    }
}
