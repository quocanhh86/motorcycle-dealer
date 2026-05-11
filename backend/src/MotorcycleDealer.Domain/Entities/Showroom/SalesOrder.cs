using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Showroom;

public sealed class SalesOrder : AuditableEntity, IDealerScoped, IImmutableFinancialEntity
{
    public SalesOrder()
    {
    }

    public SalesOrder(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string OrderNumber { get; set; } = string.Empty;
    public Guid DealerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DealerInventoryId { get; set; }
    public Guid? AppliedCustomerVoucherId { get; set; }
    public Guid SoldByStaffId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int PointsEarned { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string Status { get; set; } = "Completed";
    public string? Notes { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public Dealer? Dealer { get; set; }
    public Identity.User? Customer { get; set; }
    public DealerInventory? DealerInventory { get; set; }
    public Loyalty.CustomerVoucher? AppliedCustomerVoucher { get; set; }
    public Identity.User? SoldByStaff { get; set; }
    public Garage.CustomerVehicle? OnboardedCustomerVehicle { get; set; }
}
