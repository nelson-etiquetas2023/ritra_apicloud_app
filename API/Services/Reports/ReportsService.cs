using API.Data;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Companion;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using Shared.Dtos;

namespace API.Services.Reports
{

    public class ReportsService(ApplicationDbContext context) : IReportsService
    {
        public ApplicationDbContext Context { get; set; } = context;

        public async Task GetReportScaProducts(string Order)
        {

            var datos = Context.ScanProducts.Where(p => p.OrdenId == Order)
                .OrderBy(p => p.ProductName).ToList();


            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, $"Inventario.pdf");


            Document.Create(container => {

                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));


                    page.Header().Element(CreateHeader);
                    page.Content().Element(c => CreateBody(c, datos));
                    page.Footer().Element(CreateFooter);
                      
                });
        
                }).GeneratePdfAndShow();
            //var reportPdf = document.ShowInCompanionAsync();
            //return Results.File(reportPdf, "application/pdf", "hello.pdf");


        }
        private void CreateHeader(IContainer container)
        {
            
            container
                .Padding(1)
                .Row(fila =>
            {
                fila.Spacing(10);
                fila.RelativeItem()
                .Padding(10)  
            
                .Column(columna =>
                {

                    columna.Item().Text("Nombre de la Empresa : Fredigonni, c.a.").FontSize(10).Bold();
                    columna.Item().Text("Direccion : Zona Industrial san isidro").FontSize(10).Bold();
                    columna.Item().Text("Sistema de Inventario.").FontSize(10).Bold();
                    columna.Item().Text("Reporte de Inventario Fisico.").FontSize(10).Bold();
                    columna.Item().Text("Numero de documento: " + "10020").FontSize(10).Bold();
                    columna.Item().Text("Fecha de documento: " + "13-02-2026").FontSize(10).Bold();

                });

                
                fila.RelativeItem()
                .Padding(10)
                .Column(columna =>
                {
                    columna.Item().Text($"RCN : J-069525155-2" )
                   .AlignRight().FontSize(10).Bold();
                    columna.Item().Text($"Fecha: " + DateTime.Today.ToShortDateString())
                    .AlignRight().FontSize(10).Bold();
                    columna.Item().Text($"Hora : " + DateTime.Today.ToShortTimeString())
                    .AlignRight().FontSize(10).Bold();
                    columna.Item().Text($"Usuario : Nelson Pino")
                    .AlignRight().FontSize(10).Bold();
                    columna.Item().Text($"Email : npino.tecno2024@gmail.com")
                    .AlignRight().FontSize(10).Bold();
                    columna.Item().Text($"Descrip : Inventario diciembre 2026")
                    .AlignRight().FontSize(10).Bold();
                });
            });
        }


        private static void CreateBody(IContainer container, List<ScanProducts> scanproducts) 
        {
            
            container.Table(table =>
            {
                
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  //item
                    columns.ConstantColumn(70);  //codligo
                    columns.ConstantColumn(135); //nombre del producto
                    columns.ConstantColumn(135); //categoria
                    columns.ConstantColumn(60);  //cantidad
                    columns.ConstantColumn(70);  //UNIDAD
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(header =>
                {
                    
                    header.Cell().Element(CellStyle).Text("It");
                    header.Cell().Element(CellStyle).Text("Codigo");
                    header.Cell().Element(CellStyle).Text("Nombre del Producto"); 
                    header.Cell().Element(CellStyle).Text("Categoria"); 
                    header.Cell().Element(CellStyle).Text("Cant."); 
                    header.Cell().Element(CellStyle).Text("Unidad."); 
                    header.Cell().Element(CellStyle).Text("Ubicación"); 
                    header.Cell().Element(CellStyle).Text("Estado");
                    header.Cell().Element(CellStyle).Text("Fecha-Hora");
                    
                    static IContainer CellStyle(IContainer container) 
                    {
                        return container
                        .Background(Colors.Grey.Lighten2)
                        .DefaultTextStyle(x => x.FontColor(Colors.Black).Bold().FontSize(10))
                        .PaddingVertical(8)
                        .PaddingHorizontal(16);
                    }
                });
                
                int fila = 1;
                
                foreach (var item in scanproducts) 
                {
                    table.Cell().Text(fila.ToString()).FontSize(8).AlignCenter();
                    table.Cell().Text(item.Codebar).FontSize(8);
                    table.Cell().Text(item.ProductName).FontSize(8);
                    table.Cell().Text(item.Category).FontSize(8);
                    table.Cell().Text(item.Quantity.ToString()).FontSize(8).AlignCenter();
                    table.Cell().Text(item.Unidad) .FontSize(8).AlignCenter();
                    table.Cell().Text(item.Ubicacion).FontSize(8).AlignCenter();
                    table.Cell().Text(item.Estado).FontSize(8);
                    table.Cell().Text(item.DateScan.ToString()).FontSize(8);
                    fila ++;
                }
            });
        }

        private void CreateFooter(IContainer container) 
        {
            
            container
                .Background(Colors.Grey.Lighten1)
                .CornerRadius(5)
                .Row(row => {
                
                row.RelativeItem().Column(columna =>
                {
                    columna.Item().Text("Pagina: ").AlignCenter().Bold();
                    columna.Item().Text(text =>
                    {
                        text.AlignCenter();
                        text.CurrentPageNumber().Bold();
                        text.Span("/").Bold();
                        text.TotalPages().Bold();

                    });    
                });
                
            });
            
        }


    }
}
