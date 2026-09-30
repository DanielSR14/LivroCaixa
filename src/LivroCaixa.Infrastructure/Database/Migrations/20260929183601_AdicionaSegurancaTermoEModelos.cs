using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivroCaixa.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSegurancaTermoEModelos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacaoHash",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinutosBloqueioInatividade",
                table: "Empresas",
                type: "INTEGER",
                nullable: false,
                // Editado à mão: o gerado era 0 ("nunca bloquear") para empresas já cadastradas;
                // elas devem receber o mesmo padrão de quem instala agora (Empresa.BloqueioInatividadePadraoMinutos).
                defaultValue: 10);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermoAceitoEm",
                table: "Empresas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersaoTermoAceita",
                table: "Empresas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelosLancamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FormaPagamento = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosLancamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosLancamento_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModelosLancamento_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ModelosLancamento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosLancamento_CategoriaId",
                table: "ModelosLancamento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosLancamento_ContaId",
                table: "ModelosLancamento",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosLancamento_EmpresaId_Codigo",
                table: "ModelosLancamento",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelosLancamento");

            migrationBuilder.DropColumn(
                name: "CodigoRecuperacaoHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MinutosBloqueioInatividade",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "TermoAceitoEm",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "VersaoTermoAceita",
                table: "Empresas");
        }
    }
}
