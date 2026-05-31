using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellingCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellingCurrency",
                table: "Batches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "LKR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellingCurrency",
                table: "Batches");
        }
    }
}
