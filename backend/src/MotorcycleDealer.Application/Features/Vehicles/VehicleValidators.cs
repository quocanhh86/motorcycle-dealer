using FluentValidation;
using MotorcycleDealer.Domain.ValueObjects;

namespace MotorcycleDealer.Application.Features.Vehicles;

public sealed class RegisterExternalVehicleRequestValidator : AbstractValidator<RegisterExternalVehicleRequest>
{
    public RegisterExternalVehicleRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

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

        RuleFor(x => x.MotorcycleModelId)
            .NotEmpty()
            .WithMessage("MotorcycleModelId is required.");

        RuleFor(x => x.CurrentMileageKm)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("CurrentMileageKm must be between 0 and 1,000,000 km.");

        RuleFor(x => x.LastServiceMileageKm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("LastServiceMileageKm cannot be negative.")
            .LessThanOrEqualTo(x => x.CurrentMileageKm)
            .WithMessage("LastServiceMileageKm cannot exceed CurrentMileageKm.");

        RuleFor(x => x.LicensePlate)
            .Must(p => p!.Trim().Length >= 4)
            .When(x => !string.IsNullOrWhiteSpace(x.LicensePlate))
            .WithMessage("LicensePlate must be at least 4 characters long when supplied.")
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.LicensePlate))
            .WithMessage("LicensePlate must not exceed 20 characters.");

        RuleFor(x => x.ColorName)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.ColorName))
            .WithMessage("ColorName must not exceed 60 characters.");
    }
}

public sealed class UpdateVehicleMileageRequestValidator : AbstractValidator<UpdateVehicleMileageRequest>
{
    public UpdateVehicleMileageRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ResolveMileageKm())
            .NotNull().WithMessage("currentMileageKm is required.")
            .GreaterThanOrEqualTo(0).WithMessage("currentMileageKm cannot be negative.")
            .LessThanOrEqualTo(1_000_000).WithMessage("currentMileageKm must not exceed 1,000,000 km.")
            .OverridePropertyName("currentMileageKm");
    }
}
