using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CS_Warehouse.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemoCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InventoryBalances",
                keyColumns: new[] { "LocationId", "ProductId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "InventoryBalances",
                keyColumns: new[] { "LocationId", "ProductId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "InventoryBalances",
                keyColumns: new[] { "LocationId", "ProductId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "InventoryBalances",
                keyColumns: new[] { "LocationId", "ProductId" },
                keyValues: new object[] { 2, 4 });

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
