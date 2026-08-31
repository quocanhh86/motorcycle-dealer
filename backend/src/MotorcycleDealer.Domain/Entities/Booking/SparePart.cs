using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Enums;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class SparePart : AuditableEntity, ISoftDeletable
{
    public SparePart()
    {
    }

    public SparePart(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string PartCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SparePartCategory Category { get; set; } = SparePartCategory.EngineOil;
    public Guid? CompatibleModelId { get; set; }
    public string UnitLabel { get; set; } = "Unit";
    public decimal UnitPriceVnd { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Showroom.MotorcycleModel? CompatibleModel { get; set; }
    public ICollection<ServiceInvoiceLineItem> InvoiceLineItems { get; set; } = new List<ServiceInvoiceLineItem>();
}
