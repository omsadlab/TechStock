using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class dbChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                table: "SaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BatchItemWarrantyOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: false),
                    SellingPriceLKR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchItemWarrantyOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchItemWarrantyOptions_BatchItems_BatchItemId",
                        column: x => x.BatchItemId,
                        principalTable: "BatchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchItemWarrantyOptions_BatchItemId",
                table: "BatchItemWarrantyOptions",
                column: "BatchItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchItemWarrantyOptions");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                table: "SaleItems");
        }
    }
}
