using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivroCaixa.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenomeiaCnpjEAdicionaNomeFantasia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CnpjOuCpf",
                table: "Empresas",
                newName: "Cnpj");

            migrationBuilder.AlterColumn<string>(
                name: "Cnpj",
                table: "Empresas",
                type: "TEXT",
                maxLength: 18,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "NomeFantasia",
                table: "Empresas",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeFantasia",
                table: "Empresas");

            migrationBuilder.RenameColumn(
                name: "Cnpj",
                table: "Empresas",
                newName: "CnpjOuCpf");

            migrationBuilder.AlterColumn<string>(
                name: "CnpjOuCpf",
                table: "Empresas",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 18);
        }
    }
}
