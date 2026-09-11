using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorcycleDealer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceReviewsAndWarrantyClaimsV12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PointLedgerEntries_RefType",
                schema: "dbo",
                table: "PointLedgerEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PointLedgerEntries_TxType",
                schema: "dbo",
                table: "PointLedgerEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.CreateTable(
                name: "ServiceReviews",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ReviewNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ServiceInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    QualityTags = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BonusPointsAwarded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StaffResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RespondedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceReviews", x => x.Id);
                    table.CheckConstraint("CK_ServiceReviews_BonusPoints", "[BonusPointsAwarded] >= 0");
                    table.CheckConstraint("CK_ServiceReviews_Rating", "[Rating] >= 1 AND [Rating] <= 5");
                    table.ForeignKey(
                        name: "FK_ServiceReviews_CustomerVehicles",
                        column: x => x.CustomerVehicleId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReviews_Customers",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReviews_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReviews_RespondedByUsers",
                        column: x => x.RespondedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReviews_ServiceBookings",
                        column: x => x.ServiceBookingId,
                        principalSchema: "dbo",
                        principalTable: "ServiceBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReviews_ServiceInvoices",
                        column: x => x.ServiceInvoiceId,
                        principalSchema: "dbo",
                        principalTable: "ServiceInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ClaimNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VinSnapshot = table.Column<string>(type: "varchar(17)", unicode: false, maxLength: 17, nullable: false),
                    OdometerAtClaimKm = table.Column<int>(type: "int", nullable: false),
                    DefectCategory = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SymptomDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false, defaultValue: "Submitted"),
                    CoveredSparePartId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoveredAmountVnd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyClaims", x => x.Id);
                    table.CheckConstraint("CK_WarrantyClaims_CoveredAmount", "[CoveredAmountVnd] >= 0");
                    table.CheckConstraint("CK_WarrantyClaims_DefectCategory", "[DefectCategory] IN ('Engine', 'Transmission', 'Electrical', 'BrakesAndSuspension', 'FrameAndPaint', 'ExhaustAndFuel')");
                    table.CheckConstraint("CK_WarrantyClaims_Odometer", "[OdometerAtClaimKm] >= 0");
                    table.CheckConstraint("CK_WarrantyClaims_Status", "[Status] IN ('Submitted', 'UnderReview', 'Approved', 'Completed', 'Rejected')");
                    table.CheckConstraint("CK_WarrantyClaims_VinSnapshot", "LEN([VinSnapshot]) = 17 AND [VinSnapshot] NOT LIKE '%[^A-HJ-NPR-Z0-9]%'");
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_CustomerVehicles",
                        column: x => x.CustomerVehicleId,
                        principalSchema: "dbo",
                        principalTable: "CustomerVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Customers",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_ReviewedByUsers",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyClaims_SpareParts",
                        column: x => x.CoveredSparePartId,
                        principalSchema: "dbo",
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PointLedgerEntries_RefType",
                schema: "dbo",
                table: "PointLedgerEntries",
                sql: "[ReferenceType] IN ('SalesOrder', 'ServiceInvoice', 'CustomerVoucher', 'AdminManual', 'AccountOnboarding', 'ServiceReview')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PointLedgerEntries_TxType",
                schema: "dbo",
                table: "PointLedgerEntries",
                sql: "[TransactionType] IN ('SalesOrderEarn', 'ServiceInvoiceEarn', 'VoucherRedeem', 'VoucherExpireRefund', 'AdminAdjustment', 'WelcomeBonus', 'ServiceReviewBonus')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications",
                sql: "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed', 'TestDriveCreated', 'TestDriveStatusChanged', 'ServiceReviewSubmitted', 'WarrantyClaimCreated', 'WarrantyClaimStatusChanged')");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReviews_Customer_CreatedDesc",
                schema: "dbo",
                table: "ServiceReviews",
                columns: new[] { "CustomerId", "CreatedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReviews_CustomerVehicleId",
                schema: "dbo",
                table: "ServiceReviews",
                column: "CustomerVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReviews_Dealer_CreatedDesc",
                schema: "dbo",
                table: "ServiceReviews",
                columns: new[] { "DealerId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Rating", "BonusPointsAwarded" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReviews_RespondedByUserId",
                schema: "dbo",
                table: "ServiceReviews",
                column: "RespondedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReviews_ServiceBookingId",
                schema: "dbo",
                table: "ServiceReviews",
                column: "ServiceBookingId");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceReviews_ReviewNumber",
                schema: "dbo",
                table: "ServiceReviews",
                column: "ReviewNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceReviews_ServiceInvoiceId",
                schema: "dbo",
                table: "ServiceReviews",
                column: "ServiceInvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_CoveredSparePartId",
                schema: "dbo",
                table: "WarrantyClaims",
                column: "CoveredSparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_CustomerId",
                schema: "dbo",
                table: "WarrantyClaims",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_Dealer_Status_Created",
                schema: "dbo",
                table: "WarrantyClaims",
                columns: new[] { "DealerId", "Status", "CreatedAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ReviewedByUserId",
                schema: "dbo",
                table: "WarrantyClaims",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_Vehicle_CreatedDesc",
                schema: "dbo",
                table: "WarrantyClaims",
                columns: new[] { "CustomerVehicleId", "CreatedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "UQ_WarrantyClaims_ClaimNumber",
                schema: "dbo",
                table: "WarrantyClaims",
                column: "ClaimNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceReviews",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WarrantyClaims",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PointLedgerEntries_RefType",
                schema: "dbo",
                table: "PointLedgerEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PointLedgerEntries_TxType",
                schema: "dbo",
                table: "PointLedgerEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PointLedgerEntries_RefType",
                schema: "dbo",
                table: "PointLedgerEntries",
                sql: "[ReferenceType] IN ('SalesOrder', 'ServiceInvoice', 'CustomerVoucher', 'AdminManual', 'AccountOnboarding')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PointLedgerEntries_TxType",
                schema: "dbo",
                table: "PointLedgerEntries",
                sql: "[TransactionType] IN ('SalesOrderEarn', 'ServiceInvoiceEarn', 'VoucherRedeem', 'VoucherExpireRefund', 'AdminAdjustment', 'WelcomeBonus')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications",
                sql: "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed', 'TestDriveCreated', 'TestDriveStatusChanged')");
        }
    }
}
