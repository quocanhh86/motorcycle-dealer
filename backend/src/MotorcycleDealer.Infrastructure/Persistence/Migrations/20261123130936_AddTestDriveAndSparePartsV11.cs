using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorcycleDealer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestDriveAndSparePartsV11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.CreateTable(
                name: "SpareParts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PartCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CompatibleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitLabel = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    UnitPriceVnd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareParts", x => x.Id);
                    table.CheckConstraint("CK_SpareParts_Category", "[Category] IN ('EngineOil', 'Filter', 'Brakes', 'Transmission', 'Tires', 'Electrical', 'Accessory')");
                    table.CheckConstraint("CK_SpareParts_UnitPrice", "[UnitPriceVnd] > 0");
                    table.ForeignKey(
                        name: "FK_SpareParts_Models",
                        column: x => x.CompatibleModelId,
                        principalSchema: "dbo",
                        principalTable: "MotorcycleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestDriveBookings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BookingNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorcycleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedInventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    DriverLicenseNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ContactPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false, defaultValue: "Requested"),
                    CustomerNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StaffNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestDriveBookings", x => x.Id);
                    table.CheckConstraint("CK_TestDriveBookings_Status", "[Status] IN ('Requested', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");
                    table.CheckConstraint("CK_TestDriveBookings_TimeWindow", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_TestDriveBookings_Customer",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestDriveBookings_Dealers",
                        column: x => x.DealerId,
                        principalSchema: "dbo",
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestDriveBookings_Inventories",
                        column: x => x.AssignedInventoryId,
                        principalSchema: "dbo",
                        principalTable: "DealerInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestDriveBookings_Models",
                        column: x => x.MotorcycleModelId,
                        principalSchema: "dbo",
                        principalTable: "MotorcycleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceInvoiceLineItems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ServiceInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    SparePartId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartCodeSnapshot = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Part"),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPriceVnd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotalVnd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInvoiceLineItems", x => x.Id);
                    table.CheckConstraint("CK_ServiceInvoiceLineItems_ItemType", "[ItemType] IN ('Part', 'Labor')");
                    table.CheckConstraint("CK_ServiceInvoiceLineItems_LineNumber", "[LineNumber] >= 1");
                    table.CheckConstraint("CK_ServiceInvoiceLineItems_LineTotal", "[LineTotalVnd] = ([Quantity] * [UnitPriceVnd])");
                    table.CheckConstraint("CK_ServiceInvoiceLineItems_Quantity", "[Quantity] >= 1");
                    table.CheckConstraint("CK_ServiceInvoiceLineItems_UnitPrice", "[UnitPriceVnd] >= 0");
                    table.ForeignKey(
                        name: "FK_ServiceInvoiceLineItems_Invoices",
                        column: x => x.ServiceInvoiceId,
                        principalSchema: "dbo",
                        principalTable: "ServiceInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceInvoiceLineItems_SpareParts",
                        column: x => x.SparePartId,
                        principalSchema: "dbo",
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications",
                sql: "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed', 'TestDriveCreated', 'TestDriveStatusChanged')");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoiceLineItems_Invoice",
                schema: "dbo",
                table: "ServiceInvoiceLineItems",
                columns: new[] { "ServiceInvoiceId", "LineNumber" })
                .Annotation("SqlServer:Include", new[] { "SparePartId", "PartCodeSnapshot", "Description", "ItemType", "Quantity", "UnitPriceVnd", "LineTotalVnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoiceLineItems_SparePartId",
                schema: "dbo",
                table: "ServiceInvoiceLineItems",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceInvoiceLineItems_Invoice_Line",
                schema: "dbo",
                table: "ServiceInvoiceLineItems",
                columns: new[] { "ServiceInvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_Category_Active",
                schema: "dbo",
                table: "SpareParts",
                columns: new[] { "Category", "IsActive" },
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "PartCode", "Name", "CompatibleModelId", "UnitLabel", "UnitPriceVnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_CompatibleModelId",
                schema: "dbo",
                table: "SpareParts",
                column: "CompatibleModelId");

            migrationBuilder.CreateIndex(
                name: "UQ_SpareParts_PartCode",
                schema: "dbo",
                table: "SpareParts",
                column: "PartCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestDriveBookings_AssignedInventoryId",
                schema: "dbo",
                table: "TestDriveBookings",
                column: "AssignedInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TestDriveBookings_Customer_CreatedDesc",
                schema: "dbo",
                table: "TestDriveBookings",
                columns: new[] { "CustomerId", "CreatedAtUtc" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "BookingNumber", "DealerId", "MotorcycleModelId", "PreferredDate", "StartTime", "EndTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestDriveBookings_Dealer_Date_Status",
                schema: "dbo",
                table: "TestDriveBookings",
                columns: new[] { "DealerId", "PreferredDate", "Status" })
                .Annotation("SqlServer:Include", new[] { "BookingNumber", "CustomerId", "MotorcycleModelId", "StartTime", "EndTime", "AssignedInventoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestDriveBookings_MotorcycleModelId",
                schema: "dbo",
                table: "TestDriveBookings",
                column: "MotorcycleModelId");

            migrationBuilder.CreateIndex(
                name: "UQ_TestDriveBookings_BookingNumber",
                schema: "dbo",
                table: "TestDriveBookings",
                column: "BookingNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceInvoiceLineItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TestDriveBookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SpareParts",
                schema: "dbo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_EventType",
                schema: "dbo",
                table: "Notifications",
                sql: "[EventType] IN ('BookingCreated', 'BookingStatusChanged', 'MaintenanceReminderTriggered', 'LoyaltyPointsUpdated', 'TierUpgraded', 'SalesOrderCompleted', 'VoucherRedeemed')");
        }
    }
}
