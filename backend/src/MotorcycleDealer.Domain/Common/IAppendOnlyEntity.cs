namespace MotorcycleDealer.Domain.Common;

/// <summary>
/// Marker contract for strictly append-only audit/financial records (e.g., PointLedgerEntries)
/// that must never undergo UPDATE or DELETE persistence operations (ADR-003).
/// </summary>
public interface IAppendOnlyEntity;

/// <summary>
/// Marker contract for settled financial records (e.g., SalesOrders, ServiceInvoices, ServiceInvoiceLineItems)
/// that must never undergo UPDATE or DELETE persistence operations (ADR-003).
/// </summary>
public interface IImmutableFinancialEntity;
