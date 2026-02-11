using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Persons = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.PropertyId);
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
                name: "scanProducts",
                columns: table => new
                {
                    guid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codebar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Selection = table.Column<bool>(type: "bit", nullable: false),
                    Renglon = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DateScan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrdenId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scanProducts", x => x.guid);
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
                name: "Order_InvFisico_Header",
                columns: table => new
                {
                    OrderNumberID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Order_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description_Document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order_Hour = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Person_Create = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Items = table.Column<int>(type: "int", nullable: false),
                    EquiposConteo = table.Column<int>(type: "int", nullable: false),
                    Document_Anulado = table.Column<bool>(type: "bit", nullable: false),
                    Area_Almacen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sincro_Document = table.Column<bool>(type: "bit", nullable: false),
                    renglon = table.Column<int>(type: "int", nullable: false),
                    EquipoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_InvFisico_Header", x => x.OrderNumberID);
                    table.ForeignKey(
                        name: "FK_Order_InvFisico_Header_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id");
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
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_InvFisico_Details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_InvFisico_Details_Equipos_EquipoId",
                        column: x => x.EquipoId,
                        principalTable: "Equipos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Order_InvFisico_Details_Order_InvFisico_Header_OrderNumberID",
                        column: x => x.OrderNumberID,
                        principalTable: "Order_InvFisico_Header",
                        principalColumn: "OrderNumberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_InvFisico_Details_EquipoId",
                table: "Order_InvFisico_Details",
                column: "EquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_InvFisico_Details_OrderNumberID",
                table: "Order_InvFisico_Details",
                column: "OrderNumberID");

            migrationBuilder.CreateIndex(
                name: "IX_Order_InvFisico_Header_EquipoId",
                table: "Order_InvFisico_Header",
                column: "EquipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Order_InvFisico_Details");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "scanProducts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Order_InvFisico_Header");

            migrationBuilder.DropTable(
                name: "Equipos");
        }
    }
}
