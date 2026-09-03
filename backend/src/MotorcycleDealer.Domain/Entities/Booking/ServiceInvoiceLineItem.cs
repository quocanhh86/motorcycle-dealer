using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class ServiceInvoiceLineItem : BaseEntity, IImmutableFinancialEntity
{
    public ServiceInvoiceLineItem()
    {
    }

    public ServiceInvoiceLineItem(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid ServiceInvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid? SparePartId { get; set; }
    public string? PartCodeSnapshot { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Part";
    public int Quantity { get; set; } = 1;
    public decimal UnitPriceVnd { get; set; }
    public decimal LineTotalVnd { get; set; }

    public ServiceInvoice? ServiceInvoice { get; set; }
    public SparePart? SparePart { get; set; }
}
