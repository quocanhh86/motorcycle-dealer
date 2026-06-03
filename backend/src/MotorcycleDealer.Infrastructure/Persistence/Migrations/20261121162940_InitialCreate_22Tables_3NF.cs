using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorcycleDealer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_22Tables_3NF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Dealers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    OpeningTime = table.Column<TimeOnly>(type: "time(0)", nullable: false, defaultValueSql: "'08:00:00'"),
                    ClosingTime = table.Column<TimeOnly>(type: "time(0)", nullable: false, defaultValueSql: "'18:00:00'"),
                    DefaultSlotCapacity = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                    table.CheckConstraint("CK_Dealers_Capacity", "[DefaultSlotCapacity] BETWEEN 1 AND 50");
                    table.CheckConstraint("CK_Dealers_Coordinates", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
                    table.CheckConstraint("CK_Dealers_Hours", "[ClosingTime] > [OpeningTime]");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTiers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TierCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RankOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    MinQualifyingPoints = table.Column<int>(type: "int", nullable: false),
                    PointsMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BenefitsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTiers", x => x.Id);
                    table.CheckConstraint("CK_LoyaltyTiers_BenefitsJson", "ISJSON([BenefitsJson]) = 1");
                    table.CheckConstraint("CK_LoyaltyTiers_Code", "[TierCode] IN ('Bronze', 'Silver', 'Gold', 'Platinum')");
                    table.CheckConstraint("CK_LoyaltyTiers_MinPoints", "[MinQualifyingPoints] >= 0");
                    table.CheckConstraint("CK_LoyaltyTiers_Multiplier", "[PointsMultiplier] BETWEEN 1.00 AND 10.00");
                });

            migrationBuilder.CreateTable(
                name: "MotorcycleModels",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ModelCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    EngineDisplacementCc = table.Column<int>(type: "int", nullable: false),
                    BaseRetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: false, defaultValue: 36),
                    WarrantyMileageKm = table.Column<int>(type: "int", nullable: false, defaultValue: 30000),
                    ImageUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    SpecificationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorcycleModels", x => x.Id);
                    table.CheckConstraint("CK_MotorcycleModels_Category", "[Category] IN ('Scooter', 'Underbone', 'Sport', 'Naked', 'Adventure', 'Cruiser')");
                    table.CheckConstraint("CK_MotorcycleModels_EngineCc", "[EngineDisplacementCc] BETWEEN 49 AND 2500");
                    table.CheckConstraint("CK_MotorcycleModels_Price", "[BaseRetailPrice] > 0");
                    table.CheckConstraint("CK_MotorcycleModels_SpecsJson", "[SpecificationsJson] IS NULL OR ISJSON([SpecificationsJson]) = 1");
                    table.CheckConstraint("CK_MotorcycleModels_Warranty", "[WarrantyMonths] > 0 AND [WarrantyMileageKm] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.CheckConstraint("CK_Permissions_CodePrefix", "[Code] LIKE 'Permissions.%._%'");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.CheckConstraint("CK_Roles_Name", "[Name] IN ('Customer', 'DealerStaff', 'Admin')");
                });

            migrationBuilder.CreateTable(
                name: "ServiceTimeSlots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    BookedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTimeSlots", x => x.Id);
                    table.CheckConstraint("CK_ServiceTimeSlots_Capacity", "[MaxCapacity] >= 1 AND [BookedCount] >= 0 AND [BookedCount] <= [MaxCapacity]");
                    table.CheckConstraint("CK_ServiceTimeSlots_TimeRange", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_ServiceTimeSlots_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Email = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffEmployeeCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_DealerStaff_DealerId", "([StaffEmployeeCode] IS NULL AND [DealerId] IS NULL) OR ([StaffEmployeeCode] IS NOT NULL AND [DealerId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Users_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoucherCatalogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CodePrefix = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApplicableScope = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ServiceOnly"),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    DiscountType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    ValidDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    QuotaTotal = table.Column<int>(type: "int", nullable: true),
                    QuotaRedeemed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MinTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherCatalogs", x => x.Id);
                    table.CheckConstraint("CK_VoucherCatalogs_DiscountRules", "([DiscountType] = 'FixedAmount' AND [DiscountValue] > 0) OR ([DiscountType] = 'Percentage' AND [DiscountValue] > 0 AND [DiscountValue] <= 100 AND [MaxDiscountAmount] IS NOT NULL AND [MaxDiscountAmount] > 0)");
                    table.CheckConstraint("CK_VoucherCatalogs_MinOrder", "[MinOrderAmount] >= 0");
                    table.CheckConstraint("CK_VoucherCatalogs_PointsCost", "[PointsCost] > 0");
                    table.CheckConstraint("CK_VoucherCatalogs_Quota", "[QuotaRedeemed] >= 0 AND ([QuotaTotal] IS NULL OR ([QuotaTotal] >= 1 AND [QuotaRedeemed] <= [QuotaTotal]))");
                    table.CheckConstraint("CK_VoucherCatalogs_Scope", "[ApplicableScope] IN ('ServiceOnly', 'ShowroomOnly', 'All')");
                    table.CheckConstraint("CK_VoucherCatalogs_ValidDays", "[ValidDays] BETWEEN 1 AND 3650");
                    table.ForeignKey(
                        name: "FK_VoucherCatalogs_Tiers",
                        column: x => x.MinTierId,
                        principalSchema: "dbo",
                        principalTable: "LoyaltyTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DealerInventories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorcycleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VIN = table.Column<string>(type: "varchar(17)", unicode: false, maxLength: 17, nullable: false),
                    EngineNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ColorName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ManufactureYear = table.Column<short>(type: "smallint", nullable: false),
                    ListedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Available"),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SoldAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerInventories", x => x.Id);
                    table.CheckConstraint("CK_DealerInventories_Price", "[ListedPrice] >= 0");
                    table.CheckConstraint("CK_DealerInventories_Status", "[StockStatus] IN ('Available', 'Reserved', 'Sold', 'Transferred')");
                    table.CheckConstraint("CK_DealerInventories_VIN", "LEN([VIN]) = 17 AND [VIN] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
                    table.CheckConstraint("CK_DealerInventories_Year", "[ManufactureYear] BETWEEN 2000 AND 2100");
                    table.ForeignKey(
                        name: "FK_DealerInventories_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealerInventories_Models",
                        column: x => x.MotorcycleModelId,
                        principalSchema: "dbo",
                        principalTable: "MotorcycleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRules",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MotorcycleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MilestoneName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IntervalKm = table.Column<int>(type: "int", nullable: false),
                    IntervalMonths = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    LeadMileageKm = table.Column<int>(type: "int", nullable: false, defaultValue: 250),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    ChecklistSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRules", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRules_Intervals", "[IntervalKm] > 0 AND [IntervalMonths] > 0 AND [LeadTimeDays] >= 0 AND [LeadMileageKm] >= 0 AND [EstimatedDurationMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_MaintenanceRules_Models",
                        column: x => x.MotorcycleModelId,
                        principalSchema: "dbo",
                        principalTable: "MotorcycleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions",
                        column: x => x.PermissionId,
                        principalSchema: "dbo",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailablePoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LifetimeQualifyingPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LifetimeSpentVnd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TierUpgradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id);
                    table.CheckConstraint("CK_LoyaltyAccounts_Balances", "[AvailablePoints] >= 0 AND [LifetimeQualifyingPoints] >= 0 AND [LifetimeSpentVnd] >= 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyAccounts_Tiers",
                        column: x => x.CurrentTierId,
                        principalSchema: "dbo",
                        principalTable: "LoyaltyTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyAccounts_Users",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetDealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRole = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    EventType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_Audience", "[RecipientUserId] IS NOT NULL OR [TargetDealerId] IS NOT NULL OR [TargetRole] IS NOT NULL");
                    table.CheckConstraint("CK_Notifications_EventType", "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed')");
                    table.CheckConstraint("CK_Notifications_PayloadJson", "[PayloadJson] IS NULL OR ISJSON([PayloadJson]) = 1");
                    table.CheckConstraint("CK_Notifications_TargetRole", "[TargetRole] IS NULL OR [TargetRole] IN ('Customer', 'DealerStaff', 'Admin')");
                    table.ForeignKey(
                        name: "FK_Notifications_Dealers",
                        column: x => x.TargetDealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.RecipientUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    TokenFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    RevocationReason = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedByIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_ReplacedBy",
                        column: x => x.ReplacedByTokenId,
                        principalSchema: "dbo",
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_AssignedBy",
                        column: x => x.AssignedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerVouchers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    VoucherCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PointsSpent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false, defaultValue: "Available"),
                    RedeemedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVouchers", x => x.Id);
                    table.CheckConstraint("CK_CustomerVouchers_Dates", "[ExpiresAtUtc] > [RedeemedAtUtc]");
                    table.CheckConstraint("CK_CustomerVouchers_PointsSpent", "[PointsSpent] > 0");
                    table.CheckConstraint("CK_CustomerVouchers_Status", "[Status] IN ('Available', 'ReservedOnBooking', 'Used', 'Expired')");
                    table.ForeignKey(
                        name: "FK_CustomerVouchers_Accounts",
                        column: x => x.LoyaltyAccountId,
                        principalSchema: "dbo",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerVouchers_Catalogs",
                        column: x => x.VoucherCatalogId,
                        principalSchema: "dbo",
                        principalTable: "VoucherCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointLedgerEntries",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    LoyaltyAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PointsDelta = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointLedgerEntries", x => x.Id);
                    table.CheckConstraint("CK_PointLedgerEntries_Balance", "[BalanceAfter] >= 0");
                    table.CheckConstraint("CK_PointLedgerEntries_NonZero", "[PointsDelta] <> 0");
                    table.CheckConstraint("CK_PointLedgerEntries_RefType", "[ReferenceType] IN ('SalesOrder', 'ServiceInvoice', 'CustomerVoucher', 'AdminManual', 'AccountOnboarding')");
                    table.CheckConstraint("CK_PointLedgerEntries_TxType", "[TransactionType] IN ('SalesOrderEarn', 'ServiceInvoiceEarn', 'VoucherRedeem', 'VoucherExpireRefund', 'AdminAdjustment', 'WelcomeBonus')");
                    table.ForeignKey(
                        name: "FK_PointLedgerEntries_Accounts",
                        column: x => x.LoyaltyAccountId,
                        principalSchema: "dbo",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointLedgerEntries_Users",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrderNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerInventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedCustomerVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SoldByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PointsEarned = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PaymentMethod = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "BankTransfer"),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Completed"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.CheckConstraint("CK_SalesOrders_Amounts", "[TotalAmount] >= 0 AND [DiscountAmount] >= 0 AND [DiscountAmount] <= [TotalAmount] AND [FinalAmount] = [TotalAmount] - [DiscountAmount]");
                    table.CheckConstraint("CK_SalesOrders_PaymentMethod", "[PaymentMethod] IN ('Cash', 'BankTransfer', 'Card', 'Installment')");
                    table.CheckConstraint("CK_SalesOrders_Points", "[PointsEarned] >= 0");
                    table.CheckConstraint("CK_SalesOrders_Status", "[Status] IN ('Pending', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customer",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Inventory",
                        column: x => x.DealerInventoryId,
                        principalSchema: "dbo",
                        principalTable: "DealerInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Staff",
                        column: x => x.SoldByStaffId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Voucher",
                        column: x => x.AppliedCustomerVoucherId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerVehicles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    VIN = table.Column<string>(type: "varchar(17)", unicode: false, maxLength: 17, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorcycleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchasedSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicensePlate = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RegistrationSource = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "ShowroomSale"),
                    OwnershipStartDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CurrentMileageKm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastServiceDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastServiceMileageKm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextDueMileageKm = table.Column<int>(type: "int", nullable: false),
                    NextDueDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVehicles", x => x.Id);
                    table.CheckConstraint("CK_CustomerVehicles_Mileage", "[CurrentMileageKm] >= 0 AND [LastServiceMileageKm] >= 0 AND [CurrentMileageKm] >= [LastServiceMileageKm] AND [NextDueMileageKm] > [LastServiceMileageKm]");
                    table.CheckConstraint("CK_CustomerVehicles_Source", "[RegistrationSource] IN ('ShowroomSale', 'ExternalManual')");
                    table.CheckConstraint("CK_CustomerVehicles_VIN", "LEN([VIN]) = 17 AND [VIN] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_Models",
                        column: x => x.MotorcycleModelId,
                        principalSchema: "dbo",
                        principalTable: "MotorcycleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_SalesOrders",
                        column: x => x.PurchasedSalesOrderId,
                        principalSchema: "dbo",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerVehicles_Users",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBookings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BookingNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceTimeSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedCustomerVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ReportedNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OdometerAtBookingKm = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CancellationReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CheckedInAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBookings", x => x.Id);
                    table.CheckConstraint("CK_ServiceBookings_Odometer", "[OdometerAtBookingKm] >= 0");
                    table.CheckConstraint("CK_ServiceBookings_ServiceType", "[ServiceType] IN ('PeriodicMaintenance', 'WarrantyRepair', 'GeneralRepair', 'Inspection', 'OilAndTire')");
                    table.CheckConstraint("CK_ServiceBookings_Status", "[Status] IN ('Pending', 'Confirmed', 'CheckedIn', 'InProgress', 'Completed', 'Cancelled', 'NoShow')");
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Customer",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Slots",
                        column: x => x.ServiceTimeSlotId,
                        principalSchema: "dbo",
                        principalTable: "ServiceTimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Vehicles",
                        column: x => x.CustomerVehicleId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBookings_Vouchers",
                        column: x => x.AppliedCustomerVoucherId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceReminders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CustomerVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetMileageKm = table.Column<int>(type: "int", nullable: false),
                    DueDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    TriggerReason = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DeduplicationKey = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ResolvedBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceReminders", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceReminders_Reason", "[TriggerReason] IN ('MileageThreshold', 'DateThreshold', 'Overdue')");
                    table.CheckConstraint("CK_MaintenanceReminders_Status", "[Status] IN ('Pending', 'Sent', 'Booked', 'Completed', 'Dismissed', 'Failed')");
                    table.CheckConstraint("CK_MaintenanceReminders_TargetKm", "[TargetMileageKm] > 0");
                    table.ForeignKey(
                        name: "FK_MaintenanceReminders_Bookings",
                        column: x => x.ResolvedBookingId,
                        principalSchema: "dbo",
                        principalTable: "ServiceBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceReminders_Rules",
                        column: x => x.MaintenanceRuleId,
                        principalSchema: "dbo",
                        principalTable: "MaintenanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceReminders_Vehicles",
                        column: x => x.CustomerVehicleId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceInvoices",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    InvoiceNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ServiceBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedCustomerVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaborAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    PartsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    NetPaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdometerAtCompletionKm = table.Column<int>(type: "int", nullable: false),
                    PointsEarned = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    WorkSummaryNotes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    LineItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "BankTransfer"),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInvoices", x => x.Id);
                    table.CheckConstraint("CK_ServiceInvoices_Amounts", "[LaborAmount] >= 0 AND [PartsAmount] >= 0 AND [GrossAmount] = [LaborAmount] + [PartsAmount] AND [VoucherDiscountAmount] >= 0 AND [VoucherDiscountAmount] <= [GrossAmount] AND [NetPaidAmount] = [GrossAmount] - [VoucherDiscountAmount]");
                    table.CheckConstraint("CK_ServiceInvoices_LineItemsJson", "[LineItemsJson] IS NULL OR ISJSON([LineItemsJson]) = 1");
                    table.CheckConstraint("CK_ServiceInvoices_Odometer", "[OdometerAtCompletionKm] >= 0");
                    table.CheckConstraint("CK_ServiceInvoices_PaymentMethod", "[PaymentMethod] IN ('Cash', 'BankTransfer', 'Card', 'EWallet')");
                    table.CheckConstraint("CK_ServiceInvoices_Points", "[PointsEarned] >= 0");
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Bookings",
                        column: x => x.ServiceBookingId,
                        principalSchema: "dbo",
                        principalTable: "ServiceBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Customer",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Staff",
                        column: x => x.IssuedByStaffId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Vouchers",
                        column: x => x.AppliedCustomerVoucherId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_Customer_Active",
                schema: "dbo",
                table: "CustomerVehicles",
                columns: new[] { "CustomerId", "CreatedAtUtc" },
                descending: new[] { false, true },
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "VIN", "MotorcycleModelId", "LicensePlate", "CurrentMileageKm", "NextDueMileageKm", "NextDueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_MotorcycleModelId",
                schema: "dbo",
                table: "CustomerVehicles",
                column: "MotorcycleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVehicles_ReminderScan",
                schema: "dbo",
                table: "CustomerVehicles",
                columns: new[] { "NextDueDateUtc", "NextDueMileageKm" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "MotorcycleModelId", "VIN", "CurrentMileageKm", "LastServiceMileageKm" });

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerVehicles_LicensePlate",
                schema: "dbo",
                table: "CustomerVehicles",
                column: "LicensePlate",
                unique: true,
                filter: "[LicensePlate] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerVehicles_SalesOrder",
                schema: "dbo",
                table: "CustomerVehicles",
                column: "PurchasedSalesOrderId",
                unique: true,
                filter: "[PurchasedSalesOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerVehicles_VIN",
                schema: "dbo",
                table: "CustomerVehicles",
                column: "VIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVouchers_Account_Usable",
                schema: "dbo",
                table: "CustomerVouchers",
                columns: new[] { "LoyaltyAccountId", "Status", "ExpiresAtUtc" },
                filter: "[Status] IN ('Available', 'ReservedOnBooking')")
                .Annotation("SqlServer:Include", new[] { "VoucherCode", "VoucherCatalogId", "PointsSpent" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVouchers_VoucherCatalogId",
                schema: "dbo",
                table: "CustomerVouchers",
                column: "VoucherCatalogId");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerVouchers_VoucherCode",
                schema: "dbo",
                table: "CustomerVouchers",
                column: "VoucherCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealerInventories_Dealer_Status_Model",
                schema: "dbo",
                table: "DealerInventories",
                columns: new[] { "DealerId", "StockStatus", "MotorcycleModelId" })
                .Annotation("SqlServer:Include", new[] { "VIN", "ColorName", "ListedPrice", "ManufactureYear" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerInventories_MotorcycleModelId",
                schema: "dbo",
                table: "DealerInventories",
                column: "MotorcycleModelId");

            migrationBuilder.CreateIndex(
                name: "UQ_DealerInventories_EngineNumber",
                schema: "dbo",
                table: "DealerInventories",
                column: "EngineNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_DealerInventories_VIN",
                schema: "dbo",
                table: "DealerInventories",
                column: "VIN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Dealers_Code",
                schema: "dbo",
                table: "Dealers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyAccounts_Tier_Distribution",
                schema: "dbo",
                table: "LoyaltyAccounts",
                column: "CurrentTierId")
                .Annotation("SqlServer:Include", new[] { "AvailablePoints", "LifetimeQualifyingPoints", "LifetimeSpentVnd" });

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyAccounts_CustomerId",
                schema: "dbo",
                table: "LoyaltyAccounts",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyTiers_MinPoints",
                schema: "dbo",
                table: "LoyaltyTiers",
                column: "MinQualifyingPoints",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyTiers_RankOrder",
                schema: "dbo",
                table: "LoyaltyTiers",
                column: "RankOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyTiers_TierCode",
                schema: "dbo",
                table: "LoyaltyTiers",
                column: "TierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_MaintenanceRuleId",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "MaintenanceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_PendingDispatch",
                schema: "dbo",
                table: "MaintenanceReminders",
                columns: new[] { "Status", "DueDateUtc" },
                filter: "[Status] IN ('Pending', 'Sent')")
                .Annotation("SqlServer:Include", new[] { "CustomerVehicleId", "MaintenanceRuleId", "TargetMileageKm", "DeduplicationKey" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_ResolvedBookingId",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "ResolvedBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceReminders_Vehicle_History",
                schema: "dbo",
                table: "MaintenanceReminders",
                columns: new[] { "CustomerVehicleId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "MaintenanceRuleId", "TargetMileageKm", "Status", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_MaintenanceReminders_DedupKey",
                schema: "dbo",
                table: "MaintenanceReminders",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_MaintenanceRules_Model_Code",
                schema: "dbo",
                table: "MaintenanceRules",
                columns: new[] { "MotorcycleModelId", "MilestoneCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MotorcycleModels_Category_Active",
                schema: "dbo",
                table: "MotorcycleModels",
                columns: new[] { "Category", "BaseRetailPrice" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Brand", "ModelName", "EngineDisplacementCc", "ImageUrl" });

            migrationBuilder.CreateIndex(
                name: "UQ_MotorcycleModels_ModelCode",
                schema: "dbo",
                table: "MotorcycleModels",
                column: "ModelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Dealer_Unread",
                schema: "dbo",
                table: "Notifications",
                columns: new[] { "TargetDealerId", "CreatedAtUtc" },
                descending: new[] { false, true },
                filter: "[IsRead] = 0 AND [TargetDealerId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "EventType", "Title", "Message", "PayloadJson" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Recipient_Unread",
                schema: "dbo",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "CreatedAtUtc" },
                descending: new[] { false, true },
                filter: "[IsRead] = 0 AND [RecipientUserId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "EventType", "Title", "Message", "PayloadJson" });

            migrationBuilder.CreateIndex(
                name: "UQ_Permissions_Code",
                schema: "dbo",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointLedgerEntries_Account_CreatedDesc",
                schema: "dbo",
                table: "PointLedgerEntries",
                columns: new[] { "LoyaltyAccountId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "TransactionType", "PointsDelta", "BalanceAfter", "ReferenceType", "ReferenceId", "Notes" });

            migrationBuilder.CreateIndex(
                name: "IX_PointLedgerEntries_CreatedByUserId",
                schema: "dbo",
                table: "PointLedgerEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_PointLedgerEntries_IdempotencyKey",
                schema: "dbo",
                table: "PointLedgerEntries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                schema: "dbo",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Active",
                schema: "dbo",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" },
                descending: new[] { false, true },
                filter: "[RevokedAtUtc] IS NULL")
                .Annotation("SqlServer:Include", new[] { "TokenHash", "TokenFamilyId" });

            migrationBuilder.CreateIndex(
                name: "UQ_RefreshTokens_TokenHash",
                schema: "dbo",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "dbo",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_Name",
                schema: "dbo",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Analytics_Covering",
                schema: "dbo",
                table: "SalesOrders",
                columns: new[] { "DealerId", "CompletedAtUtc" },
                filter: "[Status] = 'Completed'")
                .Annotation("SqlServer:Include", new[] { "TotalAmount", "DiscountAmount", "FinalAmount", "PointsEarned" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_AppliedCustomerVoucherId",
                schema: "dbo",
                table: "SalesOrders",
                column: "AppliedCustomerVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Customer_CreatedDesc",
                schema: "dbo",
                table: "SalesOrders",
                columns: new[] { "CustomerId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "OrderNumber", "DealerId", "FinalAmount", "PointsEarned", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Dealer_CreatedDesc",
                schema: "dbo",
                table: "SalesOrders",
                columns: new[] { "DealerId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "OrderNumber", "CustomerId", "DealerInventoryId", "FinalAmount", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SoldByStaffId",
                schema: "dbo",
                table: "SalesOrders",
                column: "SoldByStaffId");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrders_ActiveInventory",
                schema: "dbo",
                table: "SalesOrders",
                column: "DealerInventoryId",
                unique: true,
                filter: "[Status] <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrders_OrderNumber",
                schema: "dbo",
                table: "SalesOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_Analytics_Dealer",
                schema: "dbo",
                table: "ServiceBookings",
                columns: new[] { "DealerId", "CreatedAtUtc", "Status" })
                .Annotation("SqlServer:Include", new[] { "ServiceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_Customer_CreatedDesc",
                schema: "dbo",
                table: "ServiceBookings",
                columns: new[] { "CustomerId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "BookingNumber", "DealerId", "CustomerVehicleId", "ServiceTimeSlotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_Dealer_Status_Created",
                schema: "dbo",
                table: "ServiceBookings",
                columns: new[] { "DealerId", "Status", "CreatedAtUtc" },
                descending: new[] { false, false, true })
                .Annotation("SqlServer:Include", new[] { "BookingNumber", "CustomerId", "CustomerVehicleId", "ServiceTimeSlotId", "ServiceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBookings_Slot_Active",
                schema: "dbo",
                table: "ServiceBookings",
                columns: new[] { "ServiceTimeSlotId", "Status" },
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'NoShow'")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "CustomerVehicleId", "BookingNumber" });

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceBookings_ActiveVehicleSlot",
                schema: "dbo",
                table: "ServiceBookings",
                columns: new[] { "CustomerVehicleId", "ServiceTimeSlotId" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'NoShow'");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceBookings_ActiveVoucher",
                schema: "dbo",
                table: "ServiceBookings",
                column: "AppliedCustomerVoucherId",
                unique: true,
                filter: "[AppliedCustomerVoucherId] IS NOT NULL AND [Status] <> 'Cancelled' AND [Status] <> 'NoShow'");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceBookings_BookingNumber",
                schema: "dbo",
                table: "ServiceBookings",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_Analytics_Covering",
                schema: "dbo",
                table: "ServiceInvoices",
                columns: new[] { "DealerId", "PaidAtUtc" })
                .Annotation("SqlServer:Include", new[] { "LaborAmount", "PartsAmount", "GrossAmount", "VoucherDiscountAmount", "NetPaidAmount", "PointsEarned" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_AppliedCustomerVoucherId",
                schema: "dbo",
                table: "ServiceInvoices",
                column: "AppliedCustomerVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_Customer_PaidDesc",
                schema: "dbo",
                table: "ServiceInvoices",
                columns: new[] { "CustomerId", "PaidAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "InvoiceNumber", "DealerId", "ServiceBookingId", "NetPaidAmount", "PointsEarned" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_IssuedByStaffId",
                schema: "dbo",
                table: "ServiceInvoices",
                column: "IssuedByStaffId");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceInvoices_BookingId",
                schema: "dbo",
                table: "ServiceInvoices",
                column: "ServiceBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceInvoices_InvoiceNumber",
                schema: "dbo",
                table: "ServiceInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTimeSlots_Dealer_Date_Open",
                schema: "dbo",
                table: "ServiceTimeSlots",
                columns: new[] { "DealerId", "SlotDate", "StartTime" },
                filter: "[IsBlocked] = 0")
                .Annotation("SqlServer:Include", new[] { "EndTime", "MaxCapacity", "BookedCount", "RowVersion" });

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceTimeSlots_Dealer_Window",
                schema: "dbo",
                table: "ServiceTimeSlots",
                columns: new[] { "DealerId", "SlotDate", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AssignedByUserId",
                schema: "dbo",
                table: "UserRoles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "dbo",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DealerId_Active",
                schema: "dbo",
                table: "Users",
                columns: new[] { "DealerId", "IsActive" },
                filter: "[DealerId] IS NOT NULL AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "FullName", "Email", "StaffEmployeeCode" });

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_NormalizedEmail",
                schema: "dbo",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_PhoneNumber",
                schema: "dbo",
                table: "Users",
                column: "PhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_StaffEmployeeCode",
                schema: "dbo",
                table: "Users",
                column: "StaffEmployeeCode",
                unique: true,
                filter: "[StaffEmployeeCode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherCatalogs_MinTierId",
                schema: "dbo",
                table: "VoucherCatalogs",
                column: "MinTierId");

            migrationBuilder.CreateIndex(
                name: "UQ_VoucherCatalogs_CodePrefix",
                schema: "dbo",
                table: "VoucherCatalogs",
                column: "CodePrefix",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_PointLedgerEntries_BlockMutation]
                ON [dbo].[PointLedgerEntries]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 50001, 'PointLedgerEntries is strictly append-only and immutable (ADR-003).', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_read_committed_snapshot_on = 0)
                BEGIN
                    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
                END;
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_PointLedgerEntries_BlockMutation];");

            migrationBuilder.DropTable(
                name: "MaintenanceReminders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PointLedgerEntries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceInvoices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaintenanceRules",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceBookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceTimeSlots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CustomerVehicles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DealerInventories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CustomerVouchers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MotorcycleModels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LoyaltyAccounts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "VoucherCatalogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LoyaltyTiers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Dealers",
                schema: "dbo");
        }
    }
}
