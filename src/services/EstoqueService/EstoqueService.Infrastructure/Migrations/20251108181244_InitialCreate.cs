using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EstoqueService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperacoesProcessadas",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "(now() AT TIME ZONE 'utc')"),
                    TipoOperacao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Resultado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacoesProcessadas", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Saldo = table.Column<long>(type: "bigint", precision: 18, scale: 2, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "(now() AT TIME ZONE 'utc')"),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Deletado = table.Column<bool>(type: "boolean", nullable: false),
                    DataDelecao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperacoesProcessadas_DataProcessamento",
                table: "OperacoesProcessadas",
                column: "DataProcessamento");

            migrationBuilder.CreateIndex(
                name: "IX_OperacoesProcessadas_TipoOperacao",
                table: "OperacoesProcessadas",
                column: "TipoOperacao");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Deletado",
                table: "Produtos",
                column: "Deletado");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Descricao",
                table: "Produtos",
                column: "Descricao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperacoesProcessadas");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
