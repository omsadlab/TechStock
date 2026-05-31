using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "BatchItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchItems_Barcode",
                table: "BatchItems",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BatchItems_Barcode",
                table: "BatchItems");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "BatchItems");
        }
    }
}
