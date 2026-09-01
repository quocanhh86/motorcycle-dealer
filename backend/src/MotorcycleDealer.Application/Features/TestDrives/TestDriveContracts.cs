using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.TestDrives;

public sealed record TestDriveBookingDto(
    Guid Id,
    Guid TestDriveId,
    string BookingNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid MotorcycleModelId,
    string MotorcycleModelCode,
    string MotorcycleModelName,
    Guid? AssignedInventoryId,
    string? AssignedVin,
    string? AssignedColorName,
    DateOnly PreferredDate,
    string StartTime,
    string EndTime,
    string DriverLicenseNumber,
    string ContactPhone,
    string? CustomerNotes,
    string? StaffNotes,
    string? CancellationReason,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record TestDriveListQuery(
    Guid? DealerId = null,
    DateOnly? PreferredDate = null,
    string? Status = null,
    string? Search = null,
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize) : PaginationQuery(PageNumber, PageSize);

public sealed record CreateTestDriveRequest
{
    public Guid DealerId { get; init; }
    public Guid MotorcycleModelId { get; init; }
    public Guid? ModelId { get; init; }
    public DateOnly PreferredDate { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string DriverLicenseNumber { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string? CustomerNotes { get; init; }

    public Guid ResolveEffectiveModelId() =>
        MotorcycleModelId != Guid.Empty ? MotorcycleModelId : (ModelId ?? Guid.Empty);
}

public sealed record CancelTestDriveRequest
{
    public string? Reason { get; init; }
    public string? CancellationReason { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveReason() =>
        !string.IsNullOrWhiteSpace(Reason)
            ? Reason.Trim()
            : (!string.IsNullOrWhiteSpace(CancellationReason) ? CancellationReason.Trim() : string.Empty);
}

public sealed record UpdateTestDriveStatusRequest
{
    public string? Status { get; init; }
    public string? NewStatus { get; init; }
    public Guid? AssignedInventoryId { get; init; }
    public string? StaffNotes { get; init; }
    public string? Notes { get; init; }
    public string? CancellationReason { get; init; }
    public string? Reason { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveStatus() =>
        !string.IsNullOrWhiteSpace(NewStatus)
            ? NewStatus.Trim()
            : (Status?.Trim() ?? string.Empty);

    public string? ResolveEffectiveStaffNotes() =>
        !string.IsNullOrWhiteSpace(StaffNotes)
            ? StaffNotes.Trim()
            : (!string.IsNullOrWhiteSpace(Notes) ? Notes.Trim() : null);

    public string? ResolveEffectiveCancellationReason() =>
        !string.IsNullOrWhiteSpace(CancellationReason)
            ? CancellationReason.Trim()
            : (!string.IsNullOrWhiteSpace(Reason) ? Reason.Trim() : ResolveEffectiveStaffNotes());
}

public interface ITestDriveService
{
    Task<TestDriveBookingDto> CreateTestDriveAsync(
        CreateTestDriveRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TestDriveBookingDto>> ListTestDrivesAsync(
        TestDriveListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default);

    Task<TestDriveBookingDto> GetTestDriveByIdAsync(
        Guid testDriveId,
        CancellationToken cancellationToken = default);

    Task<TestDriveBookingDto> CancelTestDriveAsync(
        Guid testDriveId,
        CancelTestDriveRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<TestDriveBookingDto> UpdateTestDriveStatusAsync(
        Guid testDriveId,
        UpdateTestDriveStatusRequest request,
        CancellationToken cancellationToken = default);
}
