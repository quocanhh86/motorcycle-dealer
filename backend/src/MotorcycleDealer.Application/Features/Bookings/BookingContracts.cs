using MotorcycleDealer.Application.Common.Models;
using ResponseMeta = MotorcycleDealer.Application.Features.Auth.ResponseMeta;

namespace MotorcycleDealer.Application.Features.Bookings;

public sealed record PagedApiEnvelope<T>(
    bool Success,
    IReadOnlyList<T> Data,
    PaginationMetadata Pagination,
    ResponseMeta Meta);

public static class PagedApiEnvelope
{
    public static PagedApiEnvelope<T> Success<T>(
        PagedResult<T> result,
        DateTimeOffset timestampUtc,
        string traceId)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PagedApiEnvelope<T>(
            Success: true,
            Data: result.Items,
            Pagination: result.Pagination,
            Meta: new ResponseMeta(TimestampUtc: timestampUtc, TraceId: traceId));
    }
}

public sealed record BookingDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    Guid CustomerVehicleId,
    string Vin,
    string MotorcycleModelName,
    string? LicensePlate,
    Guid ServiceTimeSlotId,
    DateOnly SlotDate,
    string StartTime,
    string EndTime,
    string ServiceType,
    int OdometerAtBookingKm,
    Guid? AppliedCustomerVoucherId,
    string? ReservedVoucherCode,
    string? ReportedNotes,
    string? CancellationReason,
    DateTimeOffset? CheckedInAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    Guid MotorcycleModelId);

public sealed record BookingQuery(
    Guid? DealerId = null,
    string? Status = null,
    DateOnly? SlotDate = null,
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize) : PaginationQuery(PageNumber, PageSize);

public sealed record CreateServiceBookingRequest
{
    public Guid DealerId { get; init; }
    public Guid ServiceTimeSlotId { get; init; }
    public Guid? SlotId { get; init; }
    public string? SlotRowVersion { get; init; }
    public Guid CustomerVehicleId { get; init; }
    public Guid? AppliedCustomerVoucherId { get; init; }
    public string? VoucherCode { get; init; }
    public string? VoucherRowVersion { get; init; }
    public string ServiceType { get; init; } = "PeriodicMaintenance";
    public int OdometerAtBookingKm { get; init; }
    public string? ReportedNotes { get; init; }

    public Guid ResolveEffectiveSlotId() =>
        ServiceTimeSlotId != Guid.Empty ? ServiceTimeSlotId : (SlotId ?? Guid.Empty);
}

public sealed record UpdateBookingStatusRequest
{
    public string? NewStatus { get; init; }
    public string? Status { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveStatus() =>
        !string.IsNullOrWhiteSpace(NewStatus)
            ? NewStatus.Trim()
            : (Status?.Trim() ?? string.Empty);

    public string? ResolveEffectiveReasonOrNotes() =>
        !string.IsNullOrWhiteSpace(CancellationReason)
            ? CancellationReason.Trim()
            : (!string.IsNullOrWhiteSpace(Notes) ? Notes.Trim() : null);
}

public sealed record CancelBookingRequest
{
    public string? CancellationReason { get; init; }
    public string? Reason { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveCancellationReason() =>
        !string.IsNullOrWhiteSpace(CancellationReason)
            ? CancellationReason.Trim()
            : (!string.IsNullOrWhiteSpace(Reason) ? Reason.Trim() : "Cancelled by user.");
}

public interface IServiceBookingService
{
    Task<BookingDto> CreateBookingAsync(
        CreateServiceBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BookingDto>> ListBookingsAsync(
        BookingQuery query,
        bool forceCustomerOwnScope = false,
        bool forceDealerQueueScope = false,
        CancellationToken cancellationToken = default);

    Task<BookingDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        CancelBookingRequest? request,
        CancellationToken cancellationToken = default);

    Task<BookingDto> UpdateBookingStatusAsync(
        Guid bookingId,
        UpdateBookingStatusRequest request,
        CancellationToken cancellationToken = default);
}
