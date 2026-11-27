using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private const int MaxIpLength = 45;

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;
    private readonly IValidator<RegisterCustomerRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        MotorcycleDealerDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions,
        IValidator<RegisterCustomerRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        ArgumentNullException.ThrowIfNull(jwtOptions);
        _jwtOptions = jwtOptions.Value;
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _refreshValidator = refreshValidator ?? throw new ArgumentNullException(nameof(refreshValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthTokenData> RegisterCustomerAsync(
        RegisterCustomerRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var trimmedEmail = request.Email.Trim();
        var normalizedEmail = User.NormalizeEmailValue(trimmedEmail);
        var trimmedPhone = request.PhoneNumber.Trim();
        var trimmedFullName = request.FullName.Trim();

        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                u => u.NormalizedEmail == normalizedEmail || u.Email == trimmedEmail,
                cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "An account with this email address is already registered.",
                "DUPLICATE_EMAIL");
        }

        var phoneExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.PhoneNumber == trimmedPhone, cancellationToken);

        if (phoneExists)
        {
            throw new ConflictException(
                "An account with this phone number is already registered.",
                "DUPLICATE_PHONE");
        }

        var customerRole = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == RoleNames.Customer, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                "SYSTEM_SEED_MISSING",
                "Canonical 'Customer' role is missing from database seed.");

        var bronzeTier = await _dbContext.LoyaltyTiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TierCode == "Bronze", cancellationToken)
            ?? throw new BusinessRuleViolationException(
                "SYSTEM_SEED_MISSING",
                "Canonical 'Bronze' loyalty tier is missing from database seed.");

        var grantedPermissions = await LoadRolePermissionsAsync(customerRole.Id, RoleNames.Customer, cancellationToken);
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var utcNow = _dateTimeProvider.UtcNow;

                var user = User.CreateCustomer(
                    email: trimmedEmail,
                    passwordHash: passwordHash,
                    fullName: trimmedFullName,
                    phoneNumber: trimmedPhone);
                user.LastLoginAtUtc = utcNow;

                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = customerRole.Id,
                    AssignedAtUtc = utcNow,
                    AssignedByUserId = null
                };

                var loyaltyAccount = new LoyaltyAccount
                {
                    CustomerId = user.Id,
                    CurrentTierId = bronzeTier.Id,
                    AvailablePoints = 0,
                    LifetimeQualifyingPoints = 0,
                    LifetimeSpentVnd = 0m
                };

                var issuedAccess = _jwtTokenService.GenerateAccessToken(new JwtTokenSubject(
                    UserId: user.Id,
                    Email: user.Email,
                    FullName: user.FullName,
                    Role: RoleNames.Customer,
                    DealerId: null,
                    Permissions: grantedPermissions));

                var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
                var refreshTokenHash = _jwtTokenService.ComputeRefreshTokenHash(rawRefreshToken);
                var refreshExpiresAtUtc = utcNow.AddDays(Math.Max(1, _jwtOptions.RefreshTokenDays));

                var refreshTokenEntity = RefreshToken.Create(
                    userId: user.Id,
                    tokenHash: refreshTokenHash,
                    tokenFamilyId: Guid.CreateVersion7(),
                    expiresAtUtc: refreshExpiresAtUtc,
                    createdAtUtc: utcNow,
                    createdByIp: SanitizeClientIp(clientIp));

                _dbContext.Users.Add(user);
                _dbContext.UserRoles.Add(userRole);
                _dbContext.LoyaltyAccounts.Add(loyaltyAccount);
                _dbContext.RefreshTokens.Add(refreshTokenEntity);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var currentUserDto = new CurrentUserDto(
                    UserId: user.Id,
                    Email: user.Email,
                    FullName: user.FullName,
                    PhoneNumber: user.PhoneNumber,
                    Role: RoleNames.Customer,
                    DealerId: null,
                    DealerCode: null,
                    DealerName: null,
                    StaffEmployeeCode: null,
                    TierCode: bronzeTier.TierCode,
                    TierMultiplier: bronzeTier.PointsMultiplier,
                    AvailablePoints: 0,
                    LifetimeQualifyingPoints: 0,
                    LifetimeSpentVnd: 0m,
                    Permissions: grantedPermissions,
                    PointsMultiplier: bronzeTier.PointsMultiplier);

                return BuildAuthTokenData(issuedAccess, rawRefreshToken, utcNow, currentUserDto);
            });
        }
        catch (DbUpdateException ex) when (SqlExceptionGuards.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException(
                "An account with the supplied email or phone number already exists.",
                "DUPLICATE_ACCOUNT");
        }
    }

    public async Task<AuthTokenData> LoginAsync(
        LoginRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = User.NormalizeEmailValue(request.Email);

        var user = await _dbContext.Users
            .Include(u => u.Dealer)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.LoyaltyAccount)
                .ThenInclude(la => la!.CurrentTier)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _ = _passwordHasher.VerifyPassword(request.Password, string.Empty);
            throw new AuthAuthenticationException(
                "INVALID_CREDENTIALS",
                "Invalid Credentials",
                "Invalid email address or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AuthAuthenticationException(
                "INVALID_CREDENTIALS",
                "Invalid Credentials",
                "Invalid email address or password.");
        }

        var utcNow = _dateTimeProvider.UtcNow;

        if (!user.IsActive)
        {
            await RevokeAllActiveUserTokensAsync(user.Id, "AccountDisabled", utcNow, cancellationToken);
            throw new ForbiddenAccessException(
                "This user account has been deactivated. Please contact an administrator.",
                "ACCOUNT_LOCKED");
        }

        var (roleName, permissions) = ExtractRoleAndPermissions(user);
        EnforceStaffDealerBinding(user, roleName);

        user.LastLoginAtUtc = utcNow;

        var issuedAccess = _jwtTokenService.GenerateAccessToken(new JwtTokenSubject(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: roleName,
            DealerId: user.DealerId,
            Permissions: permissions));

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.ComputeRefreshTokenHash(rawRefreshToken);
        var refreshExpiresAtUtc = utcNow.AddDays(Math.Max(1, _jwtOptions.RefreshTokenDays));

        var refreshTokenEntity = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshTokenHash,
            tokenFamilyId: Guid.CreateVersion7(),
            expiresAtUtc: refreshExpiresAtUtc,
            createdAtUtc: utcNow,
            createdByIp: SanitizeClientIp(clientIp));

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var profile = MapToCurrentUserDto(user, roleName, permissions);
        return BuildAuthTokenData(issuedAccess, rawRefreshToken, utcNow, profile);
    }

    public async Task<AuthTokenData> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var incomingHash = _jwtTokenService.ComputeRefreshTokenHash(request.RefreshToken);
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        var outcome = await executionStrategy.ExecuteAsync(
            async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var utcNow = _dateTimeProvider.UtcNow;

                var existingToken = await _dbContext.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, cancellationToken);

                if (existingToken is null)
                {
                    return RefreshOutcome.Failure(new AuthAuthenticationException(
                        "INVALID_REFRESH_TOKEN",
                        "Invalid Refresh Token",
                        "The supplied refresh token is invalid."));
                }

                // MOD-01 Replay Breach Rule: Presenting an already-rotated token compromises the entire TokenFamilyId!
                var wasAlreadyRotated =
                    existingToken.ReplacedByTokenId is not null ||
                    !string.IsNullOrWhiteSpace(existingToken.ReplacedByTokenHash) ||
                    string.Equals(existingToken.RevocationReason, "Rotated", StringComparison.Ordinal);

                if (wasAlreadyRotated)
                {
                    var revokedCount = await _dbContext.RefreshTokens
                        .Where(rt => rt.TokenFamilyId == existingToken.TokenFamilyId && rt.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                                .SetProperty(rt => rt.RevocationReason, "ReuseDetected"),
                            cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogWarning(
                        "Refresh token replay detected for UserId={UserId}, TokenFamilyId={TokenFamilyId}, Ip={ClientIp}. Revoked {RevokedCount} active family token(s).",
                        existingToken.UserId,
                        existingToken.TokenFamilyId,
                        SanitizeClientIp(clientIp) ?? "unknown",
                        revokedCount);

                    return RefreshOutcome.Failure(new AuthAuthenticationException(
                        "REFRESH_TOKEN_REUSE_DETECTED",
                        "Refresh Token Reuse Detected",
                        "Replay of an already-rotated refresh token was detected. All active tokens in this session family have been revoked."));
                }

                if (string.Equals(existingToken.RevocationReason, "AccountDisabled", StringComparison.Ordinal))
                {
                    return RefreshOutcome.Failure(new ForbiddenAccessException(
                        "This user account has been deactivated.",
                        "ACCOUNT_LOCKED"));
                }

                if (existingToken.IsRevoked)
                {
                    return RefreshOutcome.Failure(new AuthAuthenticationException(
                        "INVALID_REFRESH_TOKEN",
                        "Refresh Token Revoked",
                        "The supplied refresh token has been revoked."));
                }

                if (existingToken.IsExpired(utcNow))
                {
                    return RefreshOutcome.Failure(new AuthAuthenticationException(
                        "INVALID_REFRESH_TOKEN",
                        "Refresh Token Expired",
                        "The supplied refresh token has expired."));
                }

                var user = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(u => u.Dealer)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r!.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .Include(u => u.LoyaltyAccount)
                        .ThenInclude(la => la!.CurrentTier)
                    .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

                if (user is null || user.IsDeleted || !user.IsActive)
                {
                    await _dbContext.RefreshTokens
                        .Where(rt => rt.Id == existingToken.Id && rt.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                                .SetProperty(rt => rt.RevocationReason, "AccountDisabled"),
                            cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return RefreshOutcome.Failure(new ForbiddenAccessException(
                        "This user account has been deactivated.",
                        "ACCOUNT_LOCKED"));
                }

                var (roleName, permissions) = ExtractRoleAndPermissions(user);
                if (string.Equals(roleName, RoleNames.DealerStaff, StringComparison.Ordinal) &&
                    (user.DealerId is null ||
                     user.DealerId == Guid.Empty ||
                     string.IsNullOrWhiteSpace(user.StaffEmployeeCode) ||
                     user.Dealer is null ||
                     user.Dealer.IsDeleted ||
                     !user.Dealer.IsActive))
                {
                    await _dbContext.RefreshTokens
                        .Where(rt => rt.Id == existingToken.Id && rt.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                                .SetProperty(rt => rt.RevocationReason, "AccountDisabled"),
                            cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return RefreshOutcome.Failure(new ForbiddenAccessException(
                        "DealerStaff account is not bound to an active dealership branch.",
                        "STAFF_DEALER_UNASSIGNED"));
                }

                var issuedAccess = _jwtTokenService.GenerateAccessToken(new JwtTokenSubject(
                    UserId: user.Id,
                    Email: user.Email,
                    FullName: user.FullName,
                    Role: roleName,
                    DealerId: user.DealerId,
                    Permissions: permissions));

                var rawSuccessorToken = _jwtTokenService.GenerateRefreshToken();
                var successorTokenHash = _jwtTokenService.ComputeRefreshTokenHash(rawSuccessorToken);
                var successorExpiresAtUtc = utcNow.AddDays(Math.Max(1, _jwtOptions.RefreshTokenDays));

                // Atomic CAS rotation claim under RCSI: SQL Server acquires X row lock and re-checks RevokedAtUtc IS NULL!
                var claimedRows = await _dbContext.RefreshTokens
                    .Where(rt => rt.Id == existingToken.Id && rt.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                            .SetProperty(rt => rt.ReplacedByTokenHash, successorTokenHash)
                            .SetProperty(rt => rt.RevocationReason, "Rotated"),
                        cancellationToken);

                if (claimedRows == 0)
                {
                    var revokedRaceCount = await _dbContext.RefreshTokens
                        .Where(rt => rt.TokenFamilyId == existingToken.TokenFamilyId && rt.RevokedAtUtc == null)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                                .SetProperty(rt => rt.RevocationReason, "ReuseDetected"),
                            cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogWarning(
                        "Concurrent refresh token race/replay rejected for UserId={UserId}, TokenFamilyId={TokenFamilyId}. Revoked {RevokedCount} active family token(s).",
                        existingToken.UserId,
                        existingToken.TokenFamilyId,
                        revokedRaceCount);

                    return RefreshOutcome.Failure(new AuthAuthenticationException(
                        "REFRESH_TOKEN_REUSE_DETECTED",
                        "Refresh Token Reuse Detected",
                        "Concurrent or replayed refresh token detected. All active tokens in this session family have been revoked."));
                }

                var successorEntity = RefreshToken.Create(
                    userId: user.Id,
                    tokenHash: successorTokenHash,
                    tokenFamilyId: existingToken.TokenFamilyId,
                    expiresAtUtc: successorExpiresAtUtc,
                    createdAtUtc: utcNow,
                    createdByIp: SanitizeClientIp(clientIp));

                _dbContext.RefreshTokens.Add(successorEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _dbContext.RefreshTokens
                    .Where(rt => rt.Id == existingToken.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(rt => rt.ReplacedByTokenId, successorEntity.Id),
                        cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var profile = MapToCurrentUserDto(user, roleName, permissions);
                return RefreshOutcome.Succeeded(BuildAuthTokenData(issuedAccess, rawSuccessorToken, utcNow, profile));
            });

        if (outcome.Error is not null)
        {
            throw outcome.Error;
        }

        return outcome.Data!;
    }

    public async Task<LogoutResultDto> LogoutAsync(
        Guid currentUserId,
        LogoutRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authenticated user identifier is required.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var revokeAll = request?.RevokeAllDevices == true;
        Guid? targetFamilyId = null;

        if (!revokeAll && !string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var tokenHash = _jwtTokenService.ComputeRefreshTokenHash(request.RefreshToken);
            var matched = await _dbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    rt => rt.UserId == currentUserId && rt.TokenHash == tokenHash,
                    cancellationToken);

            if (matched is not null)
            {
                targetFamilyId = matched.TokenFamilyId;
            }
        }

        var query = _dbContext.RefreshTokens
            .Where(rt => rt.UserId == currentUserId && rt.RevokedAtUtc == null);

        if (targetFamilyId is { } familyId)
        {
            query = query.Where(rt => rt.TokenFamilyId == familyId);
        }

        var revokedCount = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                .SetProperty(rt => rt.RevocationReason, "UserLogout"),
            cancellationToken);

        return new LogoutResultDto(Revoked: true, RevokedTokenCount: revokedCount);
    }

    public async Task<CurrentUserDto> GetCurrentUserProfileAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authenticated user identifier is required.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Dealer)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.LoyaltyAccount)
                .ThenInclude(la => la!.CurrentTier)
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
            ?? throw NotFoundException.ForEntity("User", currentUserId);

        if (!user.IsActive)
        {
            throw new ForbiddenAccessException(
                "This user account has been deactivated.",
                "ACCOUNT_LOCKED");
        }

        var (roleName, permissions) = ExtractRoleAndPermissions(user);
        EnforceStaffDealerBinding(user, roleName);

        return MapToCurrentUserDto(user, roleName, permissions);
    }

    private async Task<IReadOnlyList<string>> LoadRolePermissionsAsync(
        Guid roleId,
        string fallbackRoleName,
        CancellationToken cancellationToken)
    {
        var dbPermissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId && rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        return dbPermissions.Count > 0
            ? dbPermissions
            : Permissions.GetDefaultsForRole(fallbackRoleName);
    }

    private Task<int> RevokeAllActiveUserTokensAsync(
        Guid userId,
        string reason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(rt => rt.RevokedAtUtc, utcNow)
                    .SetProperty(rt => rt.RevocationReason, reason),
                cancellationToken);

    private static (string RoleName, IReadOnlyList<string> Permissions) ExtractRoleAndPermissions(User user)
    {
        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var primaryRole = roleNames.Contains(RoleNames.Admin, StringComparer.Ordinal)
            ? RoleNames.Admin
            : roleNames.Contains(RoleNames.DealerStaff, StringComparer.Ordinal)
                ? RoleNames.DealerStaff
                : RoleNames.Customer;

        var permissionSet = user.UserRoles
            .Where(ur => ur.Role is not null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission is not null && !string.IsNullOrWhiteSpace(rp.Permission.Code))
            .Select(rp => rp.Permission!.Code)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        if (permissionSet.Count == 0)
        {
            permissionSet = Permissions.GetDefaultsForRole(primaryRole).ToList();
        }

        return (primaryRole, permissionSet);
    }

    private static void EnforceStaffDealerBinding(User user, string roleName)
    {
        if (!string.Equals(roleName, RoleNames.DealerStaff, StringComparison.Ordinal))
        {
            return;
        }

        if (user.DealerId is null ||
            user.DealerId == Guid.Empty ||
            string.IsNullOrWhiteSpace(user.StaffEmployeeCode) ||
            user.Dealer is null ||
            user.Dealer.IsDeleted ||
            !user.Dealer.IsActive)
        {
            throw new ForbiddenAccessException(
                "DealerStaff account is not bound to an active dealership branch.",
                "STAFF_DEALER_UNASSIGNED");
        }
    }

    private static CurrentUserDto MapToCurrentUserDto(
        User user,
        string roleName,
        IReadOnlyList<string> permissions)
    {
        var isStaff = string.Equals(roleName, RoleNames.DealerStaff, StringComparison.Ordinal);
        var isCustomer = string.Equals(roleName, RoleNames.Customer, StringComparison.Ordinal);

        var dealerCode = isStaff ? user.Dealer?.Code : null;
        var dealerName = isStaff ? user.Dealer?.Name : null;

        var loyalty = isCustomer ? user.LoyaltyAccount : null;
        var tierCode = isCustomer ? (loyalty?.CurrentTier?.TierCode ?? "Bronze") : null;
        decimal? tierMultiplier = isCustomer ? (loyalty?.CurrentTier?.PointsMultiplier ?? 1.00m) : null;
        decimal? pointsMultiplier = tierMultiplier;
        int? availablePoints = isCustomer ? (loyalty?.AvailablePoints ?? 0) : null;
        int? lifetimePoints = isCustomer ? (loyalty?.LifetimeQualifyingPoints ?? 0) : null;
        decimal? lifetimeSpent = isCustomer ? (loyalty?.LifetimeSpentVnd ?? 0m) : null;

        return new CurrentUserDto(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            Role: roleName,
            DealerId: user.DealerId,
            DealerCode: dealerCode,
            DealerName: dealerName,
            StaffEmployeeCode: user.StaffEmployeeCode,
            TierCode: tierCode,
            TierMultiplier: tierMultiplier,
            AvailablePoints: availablePoints,
            LifetimeQualifyingPoints: lifetimePoints,
            LifetimeSpentVnd: lifetimeSpent,
            Permissions: permissions,
            PointsMultiplier: pointsMultiplier);
    }

    private static AuthTokenData BuildAuthTokenData(
        IssuedAccessToken issuedAccess,
        string rawRefreshToken,
        DateTimeOffset utcNow,
        CurrentUserDto user)
    {
        var expiresInSeconds = (int)Math.Max(1, Math.Round((issuedAccess.ExpiresAtUtc - utcNow).TotalSeconds));
        return new AuthTokenData(
            AccessToken: issuedAccess.AccessToken,
            RefreshToken: rawRefreshToken,
            ExpiresAtUtc: issuedAccess.ExpiresAtUtc,
            ExpiresInSeconds: expiresInSeconds,
            User: user);
    }

    private static string? SanitizeClientIp(string? rawIp)
    {
        if (string.IsNullOrWhiteSpace(rawIp))
        {
            return null;
        }

        var trimmed = rawIp.Trim();
        return trimmed.Length <= MaxIpLength ? trimmed : trimmed[..MaxIpLength];
    }

    private sealed record RefreshOutcome(AuthTokenData? Data, Exception? Error)
    {
        public static RefreshOutcome Succeeded(AuthTokenData data) => new(data, null);
        public static RefreshOutcome Failure(Exception error) => new(null, error);
    }
}
