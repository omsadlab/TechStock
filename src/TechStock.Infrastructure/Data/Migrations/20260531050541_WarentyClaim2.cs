using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WarentyClaim2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplacementBatchItemId",
                table: "WarrantyClaims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReplacementCostLKR",
                table: "WarrantyClaims",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StockDeducted",
                table: "WarrantyClaims",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ReplacementBatchItemId",
                table: "WarrantyClaims",
                column: "ReplacementBatchItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_BatchItems_ReplacementBatchItemId",
                table: "WarrantyClaims",
                column: "ReplacementBatchItemId",
                principalTable: "BatchItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_BatchItems_ReplacementBatchItemId",
                table: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_ReplacementBatchItemId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ReplacementBatchItemId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ReplacementCostLKR",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "StockDeducted",
                table: "WarrantyClaims");
        }
    }
}
