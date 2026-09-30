using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivroCaixa.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNomeOriginalAnexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeOriginal",
                table: "Anexos",
                type: "TEXT",
                maxLength: 260,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeOriginal",
                table: "Anexos");
        }
    }
}
