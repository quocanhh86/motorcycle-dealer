using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotorcycleDealer.Domain.Entities.Booking;

namespace MotorcycleDealer.Infrastructure.Persistence.Configurations.Booking;

public sealed class ServiceInvoiceLineItemConfiguration : IEntityTypeConfiguration<ServiceInvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<ServiceInvoiceLineItem> builder)
    {
        builder.ToTable("ServiceInvoiceLineItems", t =>
        {
            t.HasCheckConstraint("CK_ServiceInvoiceLineItems_LineNumber", "[LineNumber] >= 1");
            t.HasCheckConstraint("CK_ServiceInvoiceLineItems_ItemType", "[ItemType] IN ('Part', 'Labor')");
            t.HasCheckConstraint("CK_ServiceInvoiceLineItems_Quantity", "[Quantity] >= 1");
            t.HasCheckConstraint("CK_ServiceInvoiceLineItems_UnitPrice", "[UnitPriceVnd] >= 0");
            t.HasCheckConstraint("CK_ServiceInvoiceLineItems_LineTotal", "[LineTotalVnd] = ([Quantity] * [UnitPriceVnd])");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceInvoiceLineItems");
        builder.Property(x => x.Id).ValueGeneratedNever().HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.PartCodeSnapshot).HasMaxLength(32).IsUnicode(false);
        builder.Property(x => x.Description).HasMaxLength(200).IsUnicode(true).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Part").IsRequired();
        builder.Property(x => x.Quantity).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.UnitPriceVnd).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LineTotalVnd).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(x => new { x.ServiceInvoiceId, x.LineNumber }, "UQ_ServiceInvoiceLineItems_Invoice_Line")
            .IsUnique()
            .HasDatabaseName("UQ_ServiceInvoiceLineItems_Invoice_Line");

        builder.HasIndex(x => new { x.ServiceInvoiceId, x.LineNumber }, "IX_ServiceInvoiceLineItems_Invoice")
            .IncludeProperties(x => new
            {
                x.SparePartId,
                x.PartCodeSnapshot,
                x.Description,
                x.ItemType,
                x.Quantity,
                x.UnitPriceVnd,
                x.LineTotalVnd
            })
            .HasDatabaseName("IX_ServiceInvoiceLineItems_Invoice");

        builder.HasOne(x => x.ServiceInvoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(x => x.ServiceInvoiceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoiceLineItems_Invoices");

        builder.HasOne(x => x.SparePart)
            .WithMany(p => p.InvoiceLineItems)
            .HasForeignKey(x => x.SparePartId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ServiceInvoiceLineItems_SpareParts");
    }
}
