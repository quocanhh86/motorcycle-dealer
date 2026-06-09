namespace MotorcycleDealer.Application.Features.Dealers;

public sealed record DealerDto(
    Guid Id,
    string Code,
    string Name,
    string City,
    string Address,
    string PhoneNumber,
    string ContactEmail,
    decimal? Latitude,
    decimal? Longitude,
    string OpeningTime,
    string ClosingTime,
    int DefaultSlotCapacity,
    int AvailableVinCount,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record UpsertDealerRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string OpeningTime { get; init; } = "08:00:00";
    public string ClosingTime { get; init; } = "18:00:00";
    public int DefaultSlotCapacity { get; init; } = 4;
    public bool IsActive { get; init; } = true;
}

public sealed record DealerStaffDto(
    Guid UserId,
    string Email,
    string FullName,
    string PhoneNumber,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string StaffEmployeeCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record CreateDealerStaffRequest
{
    public string Email { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string? TemporaryPassword { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string StaffEmployeeCode { get; init; } = string.Empty;

    public string ResolveEffectivePassword() =>
        !string.IsNullOrWhiteSpace(Password)
            ? Password
            : TemporaryPassword ?? string.Empty;
}

public sealed record UpdateStaffStatusRequest
{
    public bool? IsActive { get; init; }
    public string? Reason { get; init; }
}

public interface IDealerAdministrationService
{
    Task<IReadOnlyList<DealerDto>> ListDealersAsync(
        string? city,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<DealerDto> GetDealerByIdAsync(
        Guid dealerId,
        bool allowInactive,
        CancellationToken cancellationToken = default);

    Task<DealerDto> CreateDealerAsync(
        UpsertDealerRequest request,
        CancellationToken cancellationToken = default);

    Task<DealerDto> UpdateDealerAsync(
        Guid dealerId,
        UpsertDealerRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DealerStaffDto>> ListDealerStaffAsync(
        Guid dealerId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<DealerStaffDto> ProvisionDealerStaffAsync(
        Guid dealerId,
        CreateDealerStaffRequest request,
        Guid assignedByAdminUserId,
        CancellationToken cancellationToken = default);

    Task<DealerStaffDto> UpdateDealerStaffStatusAsync(
        Guid dealerId,
        Guid staffUserId,
        UpdateStaffStatusRequest request,
        CancellationToken cancellationToken = default);
}
