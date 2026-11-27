using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Warranty;

public static class WarrantyClaimConstants
{
    public const int MaxWarrantyMonths = 36;
    public const int MaxWarrantyMileageKm = 30_000;
    public const int MinSymptomDescriptionLength = 10;
    public const int MaxSymptomDescriptionLength = 1000;
    public const int MinResolutionNotesLength = 5;
    public const int MaxResolutionNotesLength = 1000;
    public const int MinRejectionReasonLength = 5;
    public const int MaxRejectionReasonLength = 500;
}

public sealed record WarrantyClaimDto(
    Guid Id,
    Guid ClaimId,
    string ClaimNumber,
    string Status,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    string CustomerPhoneNumber,
    Guid CustomerVehicleId,
    string Vin,
    string VinSnapshot,
    string? LicensePlate,
    Guid MotorcycleModelId,
    string MotorcycleModelCode,
    string MotorcycleModelName,
    DateTimeOffset WarrantyStartUtc,
    DateTimeOffset WarrantyExpiresAtUtc,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    int OdometerAtClaimKm,
    string DefectCategory,
    string SymptomDescription,
    Guid? CoveredSparePartId,
    string? CoveredSparePartCode,
    string? CoveredSparePartName,
    decimal CoveredAmountVnd,
    string? ResolutionNotes,
    string? RejectionReason,
    Guid? ReviewedByUserId,
    string? ReviewedByStaffName,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record WarrantyClaimListQuery(
    Guid? DealerId = null,
    Guid? VehicleId = null,
    string? Status = null,
    string? DefectCategory = null,
    string? Search = null,
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize) : PaginationQuery(PageNumber, PageSize);

public sealed record CreateWarrantyClaimRequest
{
    public Guid CustomerVehicleId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid DealerId { get; init; }
    public int OdometerAtClaimKm { get; init; }
    public string DefectCategory { get; init; } = string.Empty;
    public string SymptomDescription { get; init; } = string.Empty;

    public Guid ResolveEffectiveVehicleId() =>
        CustomerVehicleId != Guid.Empty
            ? CustomerVehicleId
            : (VehicleId is { } vid && vid != Guid.Empty ? vid : Guid.Empty);
}

public sealed record UpdateWarrantyClaimStatusRequest
{
    public string? Status { get; init; }
    public string? NewStatus { get; init; }
    public Guid? CoveredSparePartId { get; init; }
    public Guid? SparePartId { get; init; }
    public decimal? CoveredAmountVnd { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? Notes { get; init; }
    public string? RejectionReason { get; init; }
    public string? Reason { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveStatus() =>
        !string.IsNullOrWhiteSpace(NewStatus)
            ? NewStatus.Trim()
            : (Status?.Trim() ?? string.Empty);

    public Guid? ResolveEffectiveSparePartId() =>
        CoveredSparePartId is { } cid && cid != Guid.Empty
            ? cid
            : (SparePartId is { } sid && sid != Guid.Empty ? sid : null);

    public string? ResolveEffectiveResolutionNotes() =>
        !string.IsNullOrWhiteSpace(ResolutionNotes)
            ? ResolutionNotes.Trim()
            : (!string.IsNullOrWhiteSpace(Notes) ? Notes.Trim() : null);

    public string? ResolveEffectiveRejectionReason() =>
        !string.IsNullOrWhiteSpace(RejectionReason)
            ? RejectionReason.Trim()
            : (!string.IsNullOrWhiteSpace(Reason) ? Reason.Trim() : null);
}

public interface IWarrantyClaimService
{
    Task<WarrantyClaimDto> CreateWarrantyClaimAsync(
        CreateWarrantyClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WarrantyClaimDto>> ListWarrantyClaimsAsync(
        WarrantyClaimListQuery query,
        bool forceOwnCustomerOnly = false,
        CancellationToken cancellationToken = default);

    Task<WarrantyClaimDto> GetWarrantyClaimByIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);

    Task<WarrantyClaimDto> UpdateWarrantyClaimStatusAsync(
        Guid claimId,
        UpdateWarrantyClaimStatusRequest request,
        CancellationToken cancellationToken = default);
}
