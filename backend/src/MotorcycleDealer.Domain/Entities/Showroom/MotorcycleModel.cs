using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Showroom;

public sealed class MotorcycleModel : AuditableEntity, ISoftDeletable
{
    public MotorcycleModel()
    {
    }

    public MotorcycleModel(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string ModelCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int EngineDisplacementCc { get; set; }
    public decimal BaseRetailPrice { get; set; }
    public int WarrantyMonths { get; set; } = 36;
    public int WarrantyMileageKm { get; set; } = 30_000;
    public string? ImageUrl { get; set; }
    public string? SpecificationsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public ICollection<DealerInventory> Inventories { get; set; } = new List<DealerInventory>();
    public ICollection<Garage.CustomerVehicle> CustomerVehicles { get; set; } = new List<Garage.CustomerVehicle>();
    public ICollection<Garage.MaintenanceRule> MaintenanceRules { get; set; } = new List<Garage.MaintenanceRule>();
}
