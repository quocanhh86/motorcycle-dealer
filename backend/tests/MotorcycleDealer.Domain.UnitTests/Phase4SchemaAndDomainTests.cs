using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Analytics;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Dealers;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.SpareParts;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Application.Features.Warranty;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure;
using MotorcycleDealer.Infrastructure.Identity;
using MotorcycleDealer.Infrastructure.Persistence;
using MotorcycleDealer.WebApi.Security;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class Phase4SchemaAndDomainTests
{
    private static readonly string[] Expected27TableNames =
    [
        "Users",
        "Roles",
        "Permissions",
        "UserRoles",
        "RolePermissions",
        "RefreshTokens",
        "Dealers",
        "MotorcycleModels",
        "DealerInventories",
        "SalesOrders",
        "TestDriveBookings",
        "CustomerVehicles",
        "MaintenanceRules",
        "MaintenanceReminders",
        "WarrantyClaims",
        "ServiceTimeSlots",
        "ServiceBookings",
        "SpareParts",
        "ServiceInvoices",
        "ServiceInvoiceLineItems",
        "ServiceReviews",
        "LoyaltyTiers",
        "LoyaltyAccounts",
        "PointLedgerEntries",
        "VoucherCatalogs",
        "CustomerVouchers",
        "Notifications"
    ];

    private static readonly string[] Expected31Indexes =
    [
        "IX_Users_DealerId_Active",
        "IX_RefreshTokens_UserId_Active",
        "IX_MotorcycleModels_Category_Active",
        "IX_DealerInventories_Dealer_Status_Model",
        "IX_SalesOrders_Customer_CreatedDesc",
        "IX_SalesOrders_Dealer_CreatedDesc",
        "IX_SalesOrders_Analytics_Covering",
        "IX_TestDriveBookings_Dealer_Date_Status",
        "IX_TestDriveBookings_Customer_CreatedDesc",
        "IX_CustomerVehicles_Customer_Active",
        "IX_CustomerVehicles_ReminderScan",
        "IX_MaintenanceReminders_PendingDispatch",
        "IX_MaintenanceReminders_Vehicle_History",
        "IX_WarrantyClaims_Dealer_Status_Created",
        "IX_WarrantyClaims_Vehicle_CreatedDesc",
        "IX_ServiceTimeSlots_Dealer_Date_Open",
        "IX_ServiceBookings_Dealer_Status_Created",
        "IX_ServiceBookings_Slot_Active",
        "IX_ServiceBookings_Customer_CreatedDesc",
        "IX_ServiceBookings_Analytics_Dealer",
        "IX_SpareParts_Category_Active",
        "IX_ServiceInvoices_Customer_PaidDesc",
        "IX_ServiceInvoices_Analytics_Covering",
        "IX_ServiceInvoiceLineItems_Invoice",
        "IX_ServiceReviews_Dealer_CreatedDesc",
        "IX_ServiceReviews_Customer_CreatedDesc",
        "IX_LoyaltyAccounts_Tier_Distribution",
        "IX_PointLedgerEntries_Account_CreatedDesc",
        "IX_CustomerVouchers_Account_Usable",
        "IX_Notifications_Recipient_Unread",
        "IX_Notifications_Dealer_Unread"
    ];

    [Fact]
    public void EfCoreModel_ShouldMapExact27DboTables_AndAll31CanonicalIndexes()
    {
        using var dbContext = CreateDesignModelDbContext();
        var entityTypes = dbContext.Model.GetEntityTypes().ToArray();

        Assert.Equal(27, entityTypes.Length);

        var actualTables = entityTypes
            .Select(et => et.GetTableName() ?? string.Empty)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var expectedSorted = Expected27TableNames
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedSorted, actualTables);
        Assert.All(entityTypes, et => Assert.Equal("dbo", et.GetSchema()));

        var allIndexNames = entityTypes
            .SelectMany(et => et.GetIndexes())
            .Select(ix => ix.GetDatabaseName())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(31, Expected31Indexes.Length);
        foreach (var expectedIndex in Expected31Indexes)
        {
            Assert.Contains(expectedIndex, allIndexNames);
        }

        Assert.Contains("UQ_TestDriveBookings_BookingNumber", allIndexNames);
        Assert.Contains("UQ_SpareParts_PartCode", allIndexNames);
        Assert.Contains("UQ_ServiceInvoiceLineItems_Invoice_Line", allIndexNames);
        Assert.Contains("UQ_ServiceReviews_ReviewNumber", allIndexNames);
        Assert.Contains("UQ_ServiceReviews_ServiceInvoiceId", allIndexNames);
        Assert.Contains("UQ_WarrantyClaims_ClaimNumber", allIndexNames);
    }

    [Fact]
    public void EfCoreModel_ShouldEnforceDealerScoped_RowVersion_SoftDelete_AndAppendOnlyMarkers()
    {
        using var dbContext = CreateDesignModelDbContext();
        var entityTypes = dbContext.Model.GetEntityTypes().ToArray();

        Type[] expectedDealerScopedTypes =
        [
            typeof(DealerInventory),
            typeof(SalesOrder),
            typeof(TestDriveBooking),
            typeof(ServiceTimeSlot),
            typeof(ServiceBooking),
            typeof(ServiceInvoice),
            typeof(ServiceReview),
            typeof(WarrantyClaim)
        ];

        var actualDealerScopedTypes = entityTypes
            .Where(et => typeof(IDealerScoped).IsAssignableFrom(et.ClrType))
            .Select(et => et.ClrType)
            .ToHashSet();

        Assert.Equal(8, actualDealerScopedTypes.Count);
        foreach (var scopedType in expectedDealerScopedTypes)
        {
            Assert.Contains(scopedType, actualDealerScopedTypes);
            var et = dbContext.Model.FindEntityType(scopedType)!;
            Assert.NotEmpty(et.GetDeclaredQueryFilters());
        }

        Type[] expectedRowVersionTypes =
        [
            typeof(DealerInventory),
            typeof(TestDriveBooking),
            typeof(ServiceTimeSlot),
            typeof(ServiceBooking),
            typeof(ServiceReview),
            typeof(WarrantyClaim),
            typeof(LoyaltyAccount),
            typeof(CustomerVoucher)
        ];

        var actualRowVersionTypes = entityTypes
            .Where(et => typeof(IConcurrencyProtected).IsAssignableFrom(et.ClrType))
            .Select(et => et.ClrType)
            .ToHashSet();

        Assert.Equal(8, actualRowVersionTypes.Count);
        foreach (var concurrencyType in expectedRowVersionTypes)
        {
            Assert.Contains(concurrencyType, actualRowVersionTypes);
            var rowVersionProp = dbContext.Model
                .FindEntityType(concurrencyType)!
                .FindProperty(nameof(IConcurrencyProtected.RowVersion));

            Assert.NotNull(rowVersionProp);
            Assert.True(rowVersionProp.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersionProp.ValueGenerated);
        }

        Type[] expectedSoftDeletableTypes =
        [
            typeof(User),
            typeof(Dealer),
            typeof(MotorcycleModel),
            typeof(CustomerVehicle),
            typeof(SparePart),
            typeof(VoucherCatalog)
        ];

        var actualSoftDeletableTypes = entityTypes
            .Where(et => typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
            .Select(et => et.ClrType)
            .ToHashSet();

        Assert.Equal(6, actualSoftDeletableTypes.Count);
        foreach (var softType in expectedSoftDeletableTypes)
        {
            Assert.Contains(softType, actualSoftDeletableTypes);
            var et = dbContext.Model.FindEntityType(softType)!;
            Assert.NotEmpty(et.GetDeclaredQueryFilters());
        }

        Assert.True(typeof(IAppendOnlyEntity).IsAssignableFrom(typeof(PointLedgerEntry)));
        Assert.False(typeof(ISoftDeletable).IsAssignableFrom(typeof(PointLedgerEntry)));
        Assert.True(typeof(IImmutableFinancialEntity).IsAssignableFrom(typeof(SalesOrder)));
        Assert.False(typeof(ISoftDeletable).IsAssignableFrom(typeof(SalesOrder)));
        Assert.True(typeof(IImmutableFinancialEntity).IsAssignableFrom(typeof(ServiceInvoice)));
        Assert.False(typeof(ISoftDeletable).IsAssignableFrom(typeof(ServiceInvoice)));
        Assert.True(typeof(IImmutableFinancialEntity).IsAssignableFrom(typeof(ServiceInvoiceLineItem)));
        Assert.False(typeof(ISoftDeletable).IsAssignableFrom(typeof(ServiceInvoiceLineItem)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(Notification)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(MaintenanceRule)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(MaintenanceReminder)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(Role)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(Permission)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(UserRole)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(RolePermission)));
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(RefreshToken)));
    }

    [Fact]
    public void EfCoreModel_ShouldConfigureExactDecimal18Scale2ForAllCurrencyColumns_And5Scale2ForTierMultiplier()
    {
        using var dbContext = CreateDesignModelDbContext();

        (Type EntityType, string PropertyName)[] currencyColumns =
        [
            (typeof(MotorcycleModel), nameof(MotorcycleModel.BaseRetailPrice)),
            (typeof(DealerInventory), nameof(DealerInventory.ListedPrice)),
            (typeof(SalesOrder), nameof(SalesOrder.TotalAmount)),
            (typeof(SalesOrder), nameof(SalesOrder.DiscountAmount)),
            (typeof(SalesOrder), nameof(SalesOrder.FinalAmount)),
            (typeof(SparePart), nameof(SparePart.UnitPriceVnd)),
            (typeof(ServiceInvoice), nameof(ServiceInvoice.LaborAmount)),
            (typeof(ServiceInvoice), nameof(ServiceInvoice.PartsAmount)),
            (typeof(ServiceInvoice), nameof(ServiceInvoice.GrossAmount)),
            (typeof(ServiceInvoice), nameof(ServiceInvoice.VoucherDiscountAmount)),
            (typeof(ServiceInvoice), nameof(ServiceInvoice.NetPaidAmount)),
            (typeof(ServiceInvoiceLineItem), nameof(ServiceInvoiceLineItem.UnitPriceVnd)),
            (typeof(ServiceInvoiceLineItem), nameof(ServiceInvoiceLineItem.LineTotalVnd)),
            (typeof(WarrantyClaim), nameof(WarrantyClaim.CoveredAmountVnd)),
            (typeof(LoyaltyAccount), nameof(LoyaltyAccount.LifetimeSpentVnd)),
            (typeof(VoucherCatalog), nameof(VoucherCatalog.DiscountValue)),
            (typeof(VoucherCatalog), nameof(VoucherCatalog.MaxDiscountAmount)),
            (typeof(VoucherCatalog), nameof(VoucherCatalog.MinOrderAmount))
        ];

        foreach (var (clrType, propName) in currencyColumns)
        {
            var prop = dbContext.Model.FindEntityType(clrType)!.FindProperty(propName);
            Assert.NotNull(prop);
            Assert.Equal(18, prop.GetPrecision());
            Assert.Equal(2, prop.GetScale());
        }

        var multiplierProp = dbContext.Model
            .FindEntityType(typeof(LoyaltyTier))!
            .FindProperty(nameof(LoyaltyTier.PointsMultiplier));

        Assert.NotNull(multiplierProp);
        Assert.Equal(5, multiplierProp.GetPrecision());
        Assert.Equal(2, multiplierProp.GetScale());
    }

    [Fact]
    public void UserAndRefreshTokenDomainInvariants_ShouldEnforceDealerStaffBindingAndTokenLifecycle()
    {
        var utcNow = new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        var customer = User.CreateCustomer(
            "  Minh.Nguyen@Customer.vn ",
            "PBKDF2-SHA512$210000$salt$hash",
            " Nguyen Quang Minh ",
            " 0909123456 ");

        Assert.Equal("Minh.Nguyen@Customer.vn", customer.Email);
        Assert.Equal("MINH.NGUYEN@CUSTOMER.VN", customer.NormalizedEmail);
        Assert.Null(customer.DealerId);
        Assert.Null(customer.StaffEmployeeCode);
        Assert.True(customer.IsActive);

        customer.Deactivate(utcNow);
        Assert.False(customer.IsActive);
        Assert.Equal(utcNow, customer.UpdatedAtUtc);

        customer.Activate(utcNow.AddMinutes(5));
        Assert.True(customer.IsActive);

        var dealerId = Guid.CreateVersion7();
        var staff = User.CreateDealerStaff(
            "staff.hn@motorcycledealer.vn",
            "PBKDF2-SHA512$210000$salt$hash",
            "Tran Thi Lan",
            "0912000101",
            dealerId,
            " emp-hn-001 ");

        Assert.Equal(dealerId, staff.DealerId);
        Assert.Equal("EMP-HN-001", staff.StaffEmployeeCode);

        Assert.Throws<BusinessRuleViolationException>(() =>
            User.CreateDealerStaff("bad@dealer.vn", "hash", "Bad Staff", "0912000999", Guid.Empty, "EMP-01"));

        var familyId = Guid.CreateVersion7();
        var token = RefreshToken.Create(
            userId: customer.Id,
            tokenHash: "abc123hash",
            tokenFamilyId: familyId,
            expiresAtUtc: utcNow.AddDays(7),
            createdAtUtc: utcNow,
            createdByIp: "127.0.0.1");

        Assert.True(token.IsActive(utcNow));
        Assert.False(token.IsExpired(utcNow));
        Assert.False(token.IsRevoked);

        var successorId = Guid.CreateVersion7();
        token.Rotate("def456hash", utcNow.AddMinutes(15), successorId);

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(utcNow.AddMinutes(15)));
        Assert.Equal("Rotated", token.RevocationReason);
        Assert.Equal(successorId, token.ReplacedByTokenId);
        Assert.Equal("def456hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Pbkdf2PasswordHasher_AndFluentValidators_ShouldVerifyCredentialsAndInputContracts()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.HashPassword("Customer@123!");

        Assert.StartsWith("PBKDF2-SHA512$210000$", hash, StringComparison.Ordinal);
        Assert.True(hasher.VerifyPassword("Customer@123!", hash));
        Assert.False(hasher.VerifyPassword("WrongPassword@123!", hash));
        Assert.False(hasher.VerifyPassword("Customer@123!", "malformed$hash"));

        var registerValidator = new RegisterCustomerRequestValidator();
        Assert.True(registerValidator.Validate(new RegisterCustomerRequest
        {
            Email = "valid.customer@example.vn",
            Password = "Customer@123!",
            FullName = "Valid Customer",
            PhoneNumber = "0909123456"
        }).IsValid);

        Assert.False(registerValidator.Validate(new RegisterCustomerRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FullName = "A",
            PhoneNumber = "abc"
        }).IsValid);

        var dealerValidator = new UpsertDealerRequestValidator();
        Assert.False(dealerValidator.Validate(new UpsertDealerRequest
        {
            Code = "DLR-ERR-01",
            Name = "Invalid Hours Branch",
            City = "Hanoi",
            Address = "100 Street Name, Hanoi",
            PhoneNumber = "02439001101",
            ContactEmail = "err@motorcycledealer.vn",
            OpeningTime = "18:00",
            ClosingTime = "08:00",
            DefaultSlotCapacity = 4
        }).IsValid);

        var refreshValidator = new RefreshTokenRequestValidator();
        Assert.False(refreshValidator.Validate(new RefreshTokenRequest { RefreshToken = new string('a', 31) }).IsValid);
        Assert.True(refreshValidator.Validate(new RefreshTokenRequest { RefreshToken = new string('a', 32) }).IsValid);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not-a-guid", false)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("22222222-2222-2222-2222-222222222202", true)]
    public async Task PermissionAuthorizationHandler_ShouldRequireNonEmptyDealerIdClaimForDealerStaff(
        string? rawDealerIdClaim,
        bool expectedSucceeded)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimNames.Sub, Guid.CreateVersion7().ToString("D")),
            new(JwtClaimNames.Role, RoleNames.DealerStaff),
            new(JwtClaimNames.Permissions, Permissions.Bookings.ViewDealer)
        };

        if (rawDealerIdClaim is not null)
        {
            claims.Add(new Claim(JwtClaimNames.DealerId, rawDealerIdClaim));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        var httpContext = new DefaultHttpContext { User = principal };
        var requirement = new PermissionRequirement(Permissions.Bookings.ViewDealer);
        var authContext = new AuthorizationHandlerContext([requirement], principal, httpContext);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(authContext);

        Assert.Equal(expectedSucceeded, authContext.HasSucceeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9_999)]
    [InlineData(1_000_001)]
    [InlineData(99_999_999)]
    public void Pbkdf2PasswordHasher_VerifyPassword_ShouldRejectOutOfBoundsIterationCounts(int craftedIterations)
    {
        var hasher = new Pbkdf2PasswordHasher();
        var validHash = hasher.HashPassword("Customer@123!");
        var parts = validHash.Split('$');
        var tamperedHash = $"{parts[0]}${craftedIterations}${parts[2]}${parts[3]}";

        Assert.False(hasher.VerifyPassword("Customer@123!", tamperedHash));
    }

    [Theory]
    [InlineData(null, null, 1, true)]
    [InlineData(null, null, 50, true)]
    [InlineData("-90", "-180", 4, true)]
    [InlineData("90", "180", 4, true)]
    [InlineData("21.024915", null, 4, false)]
    [InlineData(null, "105.854821", 4, false)]
    [InlineData("-90.0001", "105.854821", 4, false)]
    [InlineData("90.0001", "105.854821", 4, false)]
    [InlineData("21.024915", "-180.0001", 4, false)]
    [InlineData("21.024915", "180.0001", 4, false)]
    [InlineData(null, null, 0, false)]
    [InlineData(null, null, 51, false)]
    public void UpsertDealerRequestValidator_ShouldEnforceCoordinatesAndSlotCapacityBoundaries(
        string? rawLatitude,
        string? rawLongitude,
        int slotCapacity,
        bool expectedIsValid)
    {
        var validator = new UpsertDealerRequestValidator();
        decimal? lat = rawLatitude is null
            ? null
            : decimal.Parse(rawLatitude, System.Globalization.CultureInfo.InvariantCulture);
        decimal? lng = rawLongitude is null
            ? null
            : decimal.Parse(rawLongitude, System.Globalization.CultureInfo.InvariantCulture);

        var result = validator.Validate(new UpsertDealerRequest
        {
            Code = "DLR-BND-01",
            Name = "Boundary Test Dealership",
            City = "Hanoi",
            Address = "100 Boundary Road, Hanoi",
            PhoneNumber = "02439001101",
            ContactEmail = "boundary@motorcycledealer.vn",
            Latitude = lat,
            Longitude = lng,
            OpeningTime = "08:00:00",
            ClosingTime = "18:00:00",
            DefaultSlotCapacity = slotCapacity,
            IsActive = true
        });

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData("EMP-HN1", true)]
    [InlineData(" emp-hcm-001 ", true)]
    [InlineData("EMP-ABCDEFGHIJKLMNPQRSTUVWXYZ", true)]
    [InlineData("EMP-AB", false)]
    [InlineData("EMP-ABCDEFGHIJKLMNOPQRSTUVWXYZ", false)]
    [InlineData("STF-HN-001", false)]
    [InlineData("EMP-HN_001", false)]
    [InlineData("", false)]
    public void CreateDealerStaffRequestValidator_ShouldEnforceEmployeeBadgeBoundaries(
        string candidateBadge,
        bool expectedIsValid)
    {
        var validator = new CreateDealerStaffRequestValidator();
        var result = validator.Validate(new CreateDealerStaffRequest
        {
            Email = "staff.boundary@motorcycledealer.vn",
            Password = "Staff@123!",
            FullName = "Nguyen Van Boundary",
            PhoneNumber = "0912345678",
            StaffEmployeeCode = candidateBadge
        });

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Fact]
    public void AddInfrastructure_ShouldCohesivelyRegisterAuthAndDealerAdministrationServices()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = DependencyInjection.DefaultLocalDbConnectionString,
                ["Jwt:SigningKey"] = "UnitTestOnly_Min32Byte_SigningSecret_2026_Phase4!"
            })
            .Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        MotorcycleDealer.Application.DependencyInjection.AddApplication(services);
        services.AddInfrastructure(configuration);
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new MinimalDevEnvironment());
        services.AddScoped<ICurrentUserContext, MinimalCurrentUserContext>();
        services.AddScoped<INotificationPublisher, NoOpNotificationPublisher>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuthService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDealerAdministrationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITestDriveService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISparePartService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IServiceReviewService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWarrantyClaimService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAdminAnalyticsService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IJwtTokenService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<MotorcycleDealer.Infrastructure.Persistence.Seed.DatabaseSeeder>());
    }

    [Theory]
    [InlineData(TestDriveStatus.Requested, "Requested")]
    [InlineData(TestDriveStatus.Confirmed, "Confirmed")]
    [InlineData(TestDriveStatus.Completed, "Completed")]
    [InlineData(TestDriveStatus.Cancelled, "Cancelled")]
    [InlineData(TestDriveStatus.NoShow, "NoShow")]
    public void EfCoreModel_TestDriveStatusConverter_ShouldRoundTripCanonicalStrings(
        TestDriveStatus status,
        string expectedProviderString)
    {
        using var dbContext = CreateDesignModelDbContext();
        var statusProp = dbContext.Model
            .FindEntityType(typeof(TestDriveBooking))!
            .FindProperty(nameof(TestDriveBooking.Status))!;

        Assert.Equal(typeof(string), statusProp.GetProviderClrType());
        var converter = statusProp.GetTypeMapping().Converter;
        Assert.NotNull(converter);
        Assert.Equal(expectedProviderString, converter.ConvertToProvider(status));
        Assert.Equal(status, converter.ConvertFromProvider(expectedProviderString));
    }

    [Theory]
    [InlineData(SparePartCategory.EngineOil, "EngineOil")]
    [InlineData(SparePartCategory.Filter, "Filter")]
    [InlineData(SparePartCategory.Brakes, "Brakes")]
    [InlineData(SparePartCategory.Transmission, "Transmission")]
    [InlineData(SparePartCategory.Tires, "Tires")]
    [InlineData(SparePartCategory.Electrical, "Electrical")]
    [InlineData(SparePartCategory.Accessory, "Accessory")]
    public void EfCoreModel_SparePartCategoryConverter_ShouldRoundTripCanonicalStrings(
        SparePartCategory category,
        string expectedProviderString)
    {
        using var dbContext = CreateDesignModelDbContext();
        var categoryProp = dbContext.Model
            .FindEntityType(typeof(SparePart))!
            .FindProperty(nameof(SparePart.Category))!;

        Assert.Equal(typeof(string), categoryProp.GetProviderClrType());
        var converter = categoryProp.GetTypeMapping().Converter;
        Assert.NotNull(converter);
        Assert.Equal(expectedProviderString, converter.ConvertToProvider(category));
        Assert.Equal(category, converter.ConvertFromProvider(expectedProviderString));
    }

    [Theory]
    [InlineData(WarrantyDefectCategory.Engine, "Engine")]
    [InlineData(WarrantyDefectCategory.Transmission, "Transmission")]
    [InlineData(WarrantyDefectCategory.Electrical, "Electrical")]
    [InlineData(WarrantyDefectCategory.BrakesAndSuspension, "BrakesAndSuspension")]
    [InlineData(WarrantyDefectCategory.FrameAndPaint, "FrameAndPaint")]
    [InlineData(WarrantyDefectCategory.ExhaustAndFuel, "ExhaustAndFuel")]
    public void EfCoreModel_WarrantyDefectCategoryConverter_ShouldRoundTripCanonicalStrings(
        WarrantyDefectCategory category,
        string expectedProviderString)
    {
        using var dbContext = CreateDesignModelDbContext();
        var categoryProp = dbContext.Model
            .FindEntityType(typeof(WarrantyClaim))!
            .FindProperty(nameof(WarrantyClaim.DefectCategory))!;

        Assert.Equal(typeof(string), categoryProp.GetProviderClrType());
        var converter = categoryProp.GetTypeMapping().Converter;
        Assert.NotNull(converter);
        Assert.Equal(expectedProviderString, converter.ConvertToProvider(category));
        Assert.Equal(category, converter.ConvertFromProvider(expectedProviderString));
    }

    [Theory]
    [InlineData(WarrantyClaimStatus.Submitted, "Submitted")]
    [InlineData(WarrantyClaimStatus.UnderReview, "UnderReview")]
    [InlineData(WarrantyClaimStatus.Approved, "Approved")]
    [InlineData(WarrantyClaimStatus.Completed, "Completed")]
    [InlineData(WarrantyClaimStatus.Rejected, "Rejected")]
    public void EfCoreModel_WarrantyClaimStatusConverter_ShouldRoundTripCanonicalStrings(
        WarrantyClaimStatus status,
        string expectedProviderString)
    {
        using var dbContext = CreateDesignModelDbContext();
        var statusProp = dbContext.Model
            .FindEntityType(typeof(WarrantyClaim))!
            .FindProperty(nameof(WarrantyClaim.Status))!;

        Assert.Equal(typeof(string), statusProp.GetProviderClrType());
        var converter = statusProp.GetTypeMapping().Converter;
        Assert.NotNull(converter);
        Assert.Equal(expectedProviderString, converter.ConvertToProvider(status));
        Assert.Equal(status, converter.ConvertFromProvider(expectedProviderString));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(true, null, true)]
    [InlineData(false, "Audit lock", true)]
    [InlineData(false, "OVER300", false)]
    public void UpdateStaffStatusRequestValidator_ShouldRequireExplicitIsActiveAndEnforceReasonLength(
        bool? isActive,
        string? reasonMarker,
        bool expectedIsValid)
    {
        var validator = new UpdateStaffStatusRequestValidator();
        var reason = reasonMarker == "OVER300" ? new string('R', 301) : reasonMarker;
        var result = validator.Validate(new UpdateStaffStatusRequest
        {
            IsActive = isActive,
            Reason = reason
        });

        Assert.Equal(expectedIsValid, result.IsValid);
    }

    [Theory]
    [InlineData("All", 48)]
    [InlineData(RoleNames.Customer, 20)]
    [InlineData(RoleNames.DealerStaff, 22)]
    [InlineData(RoleNames.Admin, 32)]
    public void PermissionsCatalog_RoleDefaultsAndTotalCount_ShouldMatchCanonicalMatrix(
        string targetScope,
        int expectedPermissionCount)
    {
        var resolved = string.Equals(targetScope, "All", StringComparison.Ordinal)
            ? Permissions.All
            : Permissions.GetDefaultsForRole(targetScope);

        Assert.Equal(expectedPermissionCount, resolved.Count);
        Assert.Equal(expectedPermissionCount, resolved.Distinct(StringComparer.Ordinal).Count());
        Assert.All(resolved, code => Assert.Contains(code, Permissions.All));
    }

    [Theory]
    [InlineData(NotificationEventType.BookingCreated, 1, "BookingCreated", true)]
    [InlineData(NotificationEventType.BookingStatusChanged, 2, "BookingStatusChanged", true)]
    [InlineData(NotificationEventType.MaintenanceReminderTriggered, 3, "MaintenanceReminderTriggered", true)]
    [InlineData(NotificationEventType.LoyaltyPointsUpdated, 4, "LoyaltyPointsUpdated", true)]
    [InlineData(NotificationEventType.TierUpgraded, 5, "TierUpgraded", true)]
    [InlineData(NotificationEventType.SalesOrderCompleted, 6, "SalesOrderCompleted", false)]
    [InlineData(NotificationEventType.VoucherRedeemed, 7, "VoucherRedeemed", false)]
    [InlineData(NotificationEventType.TestDriveCreated, 8, "TestDriveCreated", true)]
    [InlineData(NotificationEventType.TestDriveStatusChanged, 9, "TestDriveStatusChanged", true)]
    [InlineData(NotificationEventType.ServiceReviewSubmitted, 10, "ServiceReviewSubmitted", true)]
    [InlineData(NotificationEventType.WarrantyClaimCreated, 11, "WarrantyClaimCreated", true)]
    [InlineData(NotificationEventType.WarrantyClaimStatusChanged, 12, "WarrantyClaimStatusChanged", true)]
    public void NotificationEventType_AndSignalRClientContract_ShouldExposeAllCanonicalEvents(
        NotificationEventType eventType,
        int expectedOrdinal,
        string expectedName,
        bool isRealtimeHubMethod)
    {
        Assert.Equal(expectedOrdinal, (int)eventType);
        Assert.Equal(expectedName, eventType.ToString());
        Assert.Equal(12, Enum.GetValues<NotificationEventType>().Length);

        var hubMethods = typeof(MotorcycleDealer.WebApi.Hubs.INotificationClient)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(10, hubMethods.Count);
        Assert.Equal(isRealtimeHubMethod, hubMethods.Contains(expectedName));
    }

    private sealed class MinimalDevEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Microsoft.Extensions.Hosting.Environments.Development;
        public string ApplicationName { get; set; } = "MotorcycleDealer.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class MinimalCurrentUserContext : ICurrentUserContext
    {
        private static readonly HashSet<string> EmptyPermissions = new(StringComparer.Ordinal);

        public Guid? UserId => null;
        public string? Email => null;
        public string? Role => null;
        public Guid? DealerId => null;
        public bool IsAuthenticated => false;
        public IReadOnlySet<string> Permissions => EmptyPermissions;
        public bool HasPermission(string permission) => false;
    }

    private sealed class NoOpNotificationPublisher : INotificationPublisher
    {
        public Task PublishBookingCreatedAsync(BookingCreatedRealtimeEvent payload, Guid targetDealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishBookingStatusChangedAsync(BookingStatusChangedRealtimeEvent payload, Guid customerId, Guid dealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishMaintenanceReminderAsync(MaintenanceReminderRealtimeEvent payload, Guid customerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLoyaltyPointsUpdatedAsync(LoyaltyPointsUpdatedRealtimeEvent payload, Guid customerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishTierUpgradedAsync(TierUpgradedRealtimeEvent payload, Guid customerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishTestDriveCreatedAsync(TestDriveCreatedRealtimeEvent payload, Guid targetDealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishTestDriveStatusChangedAsync(TestDriveStatusChangedRealtimeEvent payload, Guid customerId, Guid dealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishServiceReviewSubmittedAsync(ServiceReviewSubmittedRealtimeEvent payload, Guid targetDealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishWarrantyClaimCreatedAsync(WarrantyClaimCreatedRealtimeEvent payload, Guid targetDealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishWarrantyClaimStatusChangedAsync(WarrantyClaimStatusChangedRealtimeEvent payload, Guid customerId, Guid dealerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static MotorcycleDealerDbContext CreateDesignModelDbContext()
    {
        var options = new DbContextOptionsBuilder<MotorcycleDealerDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=MotorcycleDealerDb_ModelMetadataOnly;Trusted_Connection=True",
                sql => sql.UseCompatibilityLevel(DependencyInjection.SqlServerCompatibilityLevel))
            .Options;

        return new MotorcycleDealerDbContext(options);
    }
}
