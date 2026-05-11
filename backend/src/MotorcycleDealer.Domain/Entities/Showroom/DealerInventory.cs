using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.ValueObjects;

namespace MotorcycleDealer.Domain.Entities.Showroom;

public sealed class DealerInventory : AuditableEntity, IDealerScoped, IConcurrencyProtected
{
    public DealerInventory()
    {
    }

    public DealerInventory(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid DealerId { get; set; }
    public Guid MotorcycleModelId { get; set; }
    public string VIN { get; set; } = string.Empty;
    public string EngineNumber { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public short ManufactureYear { get; set; }
    public decimal ListedPrice { get; set; }
    public string StockStatus { get; set; } = "Available";
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? SoldAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Dealer? Dealer { get; set; }
    public MotorcycleModel? MotorcycleModel { get; set; }
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public Vin GetValidatedVin() => Vin.Create(VIN);

    public void MarkSold(DateTimeOffset utcNow)
    {
        StockStatus = "Sold";
        SoldAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
