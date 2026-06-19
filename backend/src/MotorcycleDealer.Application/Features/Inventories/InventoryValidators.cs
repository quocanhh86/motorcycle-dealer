using System.Text.RegularExpressions;
using FluentValidation;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.ValueObjects;

namespace MotorcycleDealer.Application.Features.Inventories;

public static partial class InventoryValidationRules
{
    public static readonly IReadOnlySet<string> EditableStockStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Available",
        "Reserved",
        "Transferred"
    };

    public static readonly IReadOnlySet<string> AllStockStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Available",
        "Reserved",
        "Sold",
        "Transferred"
    };

    [GeneratedRegex(@"^[A-Z0-9-]{4,30}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    public static partial Regex EngineNumberRegex();

    public static bool TryNormalizeStockStatus(string? rawStatus, out string normalizedStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            normalizedStatus = string.Empty;
            return false;
        }

        var trimmed = rawStatus.Trim();
        foreach (var status in AllStockStatuses)
        {
            if (string.Equals(status, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = status;
                return true;
            }
        }

        normalizedStatus = string.Empty;
        return false;
    }
}

public sealed class CreateDealerInventoryRequestValidator : AbstractValidator<CreateDealerInventoryRequest>
{
    public CreateDealerInventoryRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.MotorcycleModelId)
            .NotEmpty()
            .WithMessage("MotorcycleModelId is required.");

        RuleFor(x => x.Vin)
            .NotEmpty().WithMessage("VIN is required.")
            .Custom((rawVin, ctx) =>
            {
                var normalized = rawVin.Trim().ToUpperInvariant();
                var result = Vin.CreateResult(normalized);
                if (result.IsFailure)
                {
                    ctx.AddFailure(result.Error.Description);
                }
            });

        RuleFor(x => x.EngineNumber)
            .NotEmpty().WithMessage("Engine number is required.")
            .MaximumLength(30).WithMessage("Engine number must not exceed 30 characters.")
            .Must(e => InventoryValidationRules.EngineNumberRegex().IsMatch(e.Trim().ToUpperInvariant()))
            .WithMessage("Engine number must contain 4 to 30 uppercase alphanumeric or hyphen characters.");

        RuleFor(x => x.ColorName)
            .NotEmpty().WithMessage("Color name is required.")
            .Must(c => c.Trim().Length >= 2).WithMessage("Color name must be at least 2 characters long.")
            .MaximumLength(60).WithMessage("Color name must not exceed 60 characters.");

        RuleFor(x => x.ManufactureYear)
            .InclusiveBetween(2000, 2100)
            .WithMessage("ManufactureYear must be between 2000 and 2100.");

        RuleFor(x => x.ListedPrice)
            .GreaterThan(0m)
            .WithMessage("ListedPrice must be strictly greater than 0 VND.");
    }
}

public sealed class UpdateDealerInventoryRequestValidator : AbstractValidator<UpdateDealerInventoryRequest>
{
    public UpdateDealerInventoryRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion concurrency token is required.")
            .Must(ShowroomValidationRules.IsValidRowVersionBase64)
            .WithMessage("RowVersion must be a valid Base64-encoded 8-byte SQL Server ROWVERSION token.");

        RuleFor(x => x.StockStatus)
            .Must(s => InventoryValidationRules.TryNormalizeStockStatus(s, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.StockStatus))
            .WithMessage("StockStatus must be one of: Available, Reserved, Sold, Transferred.");

        RuleFor(x => x.ListedPrice)
            .GreaterThan(0m)
            .When(x => x.ListedPrice.HasValue)
            .WithMessage("ListedPrice must be strictly greater than 0 VND.");

        RuleFor(x => x.ColorName)
            .Must(c => c!.Trim().Length >= 2)
            .When(x => !string.IsNullOrWhiteSpace(x.ColorName))
            .WithMessage("Color name must be at least 2 characters long.")
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.ColorName))
            .WithMessage("Color name must not exceed 60 characters.");

        RuleFor(x => x)
            .Must(req =>
                !string.IsNullOrWhiteSpace(req.StockStatus) ||
                req.ListedPrice.HasValue ||
                !string.IsNullOrWhiteSpace(req.ColorName))
            .WithName("request")
            .WithMessage("At least one mutable field (stockStatus, listedPrice, or colorName) must be supplied.");
    }
}
