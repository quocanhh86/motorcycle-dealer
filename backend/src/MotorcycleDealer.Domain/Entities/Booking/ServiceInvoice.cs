using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Booking;

public sealed class ServiceInvoice : BaseEntity, IDealerScoped, IImmutableFinancialEntity
{
    public ServiceInvoice()
    {
    }

    public ServiceInvoice(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ServiceBookingId { get; set; }
    public Guid DealerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AppliedCustomerVoucherId { get; set; }
    public Guid IssuedByStaffId { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal PartsAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal VoucherDiscountAmount { get; set; }
    public decimal NetPaidAmount { get; set; }
    public int OdometerAtCompletionKm { get; set; }
    public int PointsEarned { get; set; }
    public string? WorkSummaryNotes { get; set; }
    public string? LineItemsJson { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public DateTimeOffset PaidAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ServiceBooking? ServiceBooking { get; set; }
    public Showroom.Dealer? Dealer { get; set; }
    public Identity.User? Customer { get; set; }
    public Loyalty.CustomerVoucher? AppliedCustomerVoucher { get; set; }
    public Identity.User? IssuedByStaff { get; set; }
    public ICollection<ServiceInvoiceLineItem> LineItems { get; set; } = new List<ServiceInvoiceLineItem>();
}
