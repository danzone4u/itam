using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMerkTypeFromBarang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Merk",
                table: "Barangs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Barangs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Merk",
                table: "Barangs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Barangs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
