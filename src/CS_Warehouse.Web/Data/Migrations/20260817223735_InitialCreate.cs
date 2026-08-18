using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CS_Warehouse.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ReorderLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Product_ReorderLevel_NonNegative", "ReorderLevel >= 0");
                    table.CheckConstraint("CK_Product_UnitCost_NonNegative", "CAST(UnitCost AS REAL) >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => new { x.ProductId, x.LocationId });
                    table.CheckConstraint("CK_InventoryBalance_QuantityOnHand_NonNegative", "QuantityOnHand >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityChange = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovement_Quantity_Positive", "Quantity > 0");
                    table.CheckConstraint("CK_StockMovement_QuantityChange_NonZero", "QuantityChange <> 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Electronics" },
                    { 2, "Office supplies" },
                    { 3, "Safety" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "MAIN", "Main floor" },
                    { 2, "OVERFLOW", "Overflow storage" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "Description", "IsArchived", "Name", "ReorderLevel", "Sku", "UnitCost" },
                values: new object[,]
                {
                    { 1, 1, "Seven-port USB-C desk hub", false, "USB-C hub", 10, "ELEC-100", 29.99m },
                    { 2, 2, "Thermal 4 x 6 inch labels", false, "Shipping label roll", 20, "OFF-210", 8.50m },
                    { 3, 3, "Powder-free gloves, box of 100", false, "Nitrile gloves", 15, "SAFE-310", 12.75m },
                    { 4, 1, "USB handheld barcode scanner", false, "Barcode scanner", 3, "ELEC-120", 64.99m }
                });

            migrationBuilder.InsertData(
                table: "InventoryBalances",
                columns: new[] { "LocationId", "ProductId", "QuantityOnHand", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, 1, 7, new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 1, 2, 45, new DateTime(2026, 8, 3, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 1, 3, 0, new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 4, 2, new DateTime(2026, 8, 2, 9, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "StockMovements",
                columns: new[] { "Id", "LocationId", "Note", "OccurredAtUtc", "ProductId", "Quantity", "QuantityChange", "Type" },
                values: new object[,]
                {
                    { 1, 1, "Opening balance", new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Utc), 1, 7, 7, "Receive" },
                    { 2, 1, "Opening delivery", new DateTime(2026, 8, 2, 9, 0, 0, 0, DateTimeKind.Utc), 2, 50, 50, "Receive" },
                    { 3, 1, "Packed customer orders", new DateTime(2026, 8, 3, 9, 0, 0, 0, DateTimeKind.Utc), 2, 5, -5, "Issue" },
                    { 4, 2, "Opening balance", new DateTime(2026, 8, 2, 9, 0, 0, 0, DateTimeKind.Utc), 4, 2, 2, "Receive" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_LocationId",
                table: "InventoryBalances",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsArchived_Name",
                table: "Products",
                columns: new[] { "IsArchived", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LocationId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "LocationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "ProductId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBalances");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
