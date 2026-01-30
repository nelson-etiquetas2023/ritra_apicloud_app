using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Order_InvFisico_Header",
                columns: table => new
                {
                    OrderNumberID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Order_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Order_Hour = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Person_Create = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    Status_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Items = table.Column<int>(type: "int", nullable: false),
                    Document_Anulado = table.Column<bool>(type: "bit", nullable: false),
                    Area_Almacen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sincro_Document = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_InvFisico_Header", x => x.OrderNumberID);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Product_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Product_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Product_Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Product_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Order_InvFisico_Details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumberID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Renglon_Id = table.Column<int>(type: "int", nullable: false),
                    Product_id = table.Column<int>(type: "int", nullable: false),
                    Product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Product_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Roll_Id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code_Unique = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width_Fisico = table.Column<double>(type: "float", nullable: false),
                    Length_Fisico = table.Column<double>(type: "float", nullable: false),
                    Width_Sistema = table.Column<double>(type: "float", nullable: false),
                    Length_Sistema = table.Column<double>(type: "float", nullable: false),
                    Width_Dif = table.Column<double>(type: "float", nullable: false),
                    Length_Dif = table.Column<double>(type: "float", nullable: false),
                    Product_Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_InvFisico_Details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_InvFisico_Details_Order_InvFisico_Header_OrderNumberID",
                        column: x => x.OrderNumberID,
                        principalTable: "Order_InvFisico_Header",
                        principalColumn: "OrderNumberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_InvFisico_Details_OrderNumberID",
                table: "Order_InvFisico_Details",
                column: "OrderNumberID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Order_InvFisico_Details");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Order_InvFisico_Header");
        }
    }
}
