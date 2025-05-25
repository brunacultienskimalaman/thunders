using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thunders.TechTest.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pracas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pracas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Relatorios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoRelatorio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParametrosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultadoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataProcessamento = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETDATE()"),
                    TempoProcessamento = table.Column<int>(type: "int", nullable: true),
                    IdSolicitacao = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MensagemErro = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatorios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilizacoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataUtilizacao = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PracaId = table.Column<int>(type: "int", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TipoVeiculo = table.Column<int>(type: "int", nullable: false),
                    DataInsercao = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utilizacoes_Pracas_PracaId",
                        column: x => x.PracaId,
                        principalTable: "Pracas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Praca_Estado_Cidade",
                table: "Pracas",
                columns: new[] { "Estado", "Cidade" });

            migrationBuilder.CreateIndex(
                name: "IX_IdSolicitacao",
                table: "Relatorios",
                column: "IdSolicitacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipoRelatorio_Data",
                table: "Relatorios",
                columns: new[] { "TipoRelatorio", "DataProcessamento" });

            migrationBuilder.CreateIndex(
                name: "IX_DataUtilizacao_Cidade",
                table: "Utilizacoes",
                columns: new[] { "DataUtilizacao", "Cidade" });

            migrationBuilder.CreateIndex(
                name: "IX_Estado_Cidade",
                table: "Utilizacoes",
                columns: new[] { "Estado", "Cidade" });

            migrationBuilder.CreateIndex(
                name: "IX_PracaId_Mes",
                table: "Utilizacoes",
                columns: new[] { "PracaId", "DataUtilizacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Relatorios");

            migrationBuilder.DropTable(
                name: "Utilizacoes");

            migrationBuilder.DropTable(
                name: "Pracas");
        }
    }
}
