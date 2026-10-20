using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScanProMovil.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickingOrders",
                columns: table => new
                {
                    PickingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumeroPedidoVenta = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioAsignado = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCompletado = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingOrders", x => x.PickingId);
                });

            migrationBuilder.CreateTable(
                name: "PickingOrderItems",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PickingId = table.Column<int>(type: "INTEGER", nullable: false),
                    PickingOrderPickingId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Completado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingOrderItems", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_PickingOrderItems_PickingOrders_PickingId",
                        column: x => x.PickingId,
                        principalTable: "PickingOrders",
                        principalColumn: "PickingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickingOrderItems_PickingOrders_PickingOrderPickingId",
                        column: x => x.PickingOrderPickingId,
                        principalTable: "PickingOrders",
                        principalColumn: "PickingId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderItems_PickingId",
                table: "PickingOrderItems",
                column: "PickingId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrderItems_PickingOrderPickingId",
                table: "PickingOrderItems",
                column: "PickingOrderPickingId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_Estado",
                table: "PickingOrders",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_PickingOrders_NumeroPedidoVenta",
                table: "PickingOrders",
                column: "NumeroPedidoVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickingOrderItems");

            migrationBuilder.DropTable(
                name: "PickingOrders");
        }
    }
}
