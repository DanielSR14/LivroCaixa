using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivroCaixa.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAuditoriaEBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExcluidoEm",
                table: "Lancamentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracoesBackup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Frequencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PastaDestino = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UltimoBackupEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesBackup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesBackup_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricosLancamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LancamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoEvento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DadosAnteriores = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosLancamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosLancamento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricosLancamento_Lancamentos_LancamentoId",
                        column: x => x.LancamentoId,
                        principalTable: "Lancamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesBackup_EmpresaId",
                table: "ConfiguracoesBackup",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosLancamento_EmpresaId_DataHora",
                table: "HistoricosLancamento",
                columns: new[] { "EmpresaId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosLancamento_LancamentoId",
                table: "HistoricosLancamento",
                column: "LancamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesBackup");

            migrationBuilder.DropTable(
                name: "HistoricosLancamento");

            migrationBuilder.DropColumn(
                name: "ExcluidoEm",
                table: "Lancamentos");
        }
    }
}
