using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivroCaixa.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaRemessaExportacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemessasExportacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ano = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    GeradoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuantidadeLancamentos = table.Column<int>(type: "INTEGER", nullable: false),
                    CaminhoArquivoGerado = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemessasExportacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemessasExportacao_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemessasExportacao_EmpresaId_Ano_Mes",
                table: "RemessasExportacao",
                columns: new[] { "EmpresaId", "Ano", "Mes" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemessasExportacao");
        }
    }
}
