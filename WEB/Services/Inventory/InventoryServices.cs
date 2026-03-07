using ClosedXML.Excel;
using Microsoft.JSInterop;
using Shared.Dtos;
using System.Text;
using System.Text.Json;

namespace WEB.Services.Inventory
{
    public class InventoryServices : IInventoryServices
    {
        IHttpClientFactory httpFactory { get; set; }
        private readonly IJSRuntime JS;
        

        private static readonly JsonSerializerOptions jsonOptions = 
            new JsonSerializerOptions() { 
                PropertyNameCaseInsensitive = true, 
                WriteIndented = true};
        
        public InventoryServices(IHttpClientFactory httpFactory, IJSRuntime JS)
        {
            this.httpFactory = httpFactory;
            this.JS = JS;
        }

        public async Task<bool> SaveNumberConsecInventory(string number, string filter)
        {
            //uso de tuplas para mandar dos parametros

            var parametros = new NumeroFiltro(number, filter);
            var url = $"api/orderfisico/savenumberconsecinventory";
            var json = JsonSerializer.Serialize(parametros, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<DocumentSettings> LoadDataDocumentSetting(string filter)
        {
            var url = $"api/orderfisico/getconfigbyid/{filter}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new DocumentSettings();
            var setting = await JsonSerializer.DeserializeAsync<DocumentSettings>(
               new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);

            return setting ?? new DocumentSettings();
        }
        public async Task ExportTxt(List<ScanProducts> data)
        {
            try
            {
                string carpetaDestino = Path.Combine(Environment.CurrentDirectory, "datos");
                if (!Directory.Exists(carpetaDestino)) 
                {
                    Directory.CreateDirectory(carpetaDestino);   
                }
                string filePath = Path.Combine(carpetaDestino, "datos.txt");

                using var stream = new MemoryStream(); 
                using (StreamWriter sr = new(stream, Encoding.UTF8, leaveOpen:true)) 
                {
                    foreach (var item in data) 
                    {
                        sr.WriteLine(item.Codebar + "," + item.ProductName + "," 
                            + item.Category +","+item.Quantity+"," + item.Unidad+"," 
                            + item.DateScan+","+item.Ubicacion+","+item.Estado);
                    }
                }
                var bytes = stream.ToArray();
                //llamada Javacript para descargar el TXT.
                await JS.InvokeVoidAsync("downloadFileTxt", "datos.txt", Convert.ToBase64String(bytes));
            }
            catch (Exception)
            {
                
            }
        }
        public async Task ExportExcel(List<ScanProducts> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventario");
            string filepath = Path.Combine(Environment.CurrentDirectory, "ExportData");
            //encabezado de la hoja de excel.
            worksheet.Cell(1, 1).Value = "It.";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 2).Value = "Codigo";
            worksheet.Cell(1, 2).Style.Font.Bold = true;
            worksheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 3).Value = "Nombre del Producto";
            worksheet.Cell(1, 3).Style.Font.Bold = true;
            worksheet.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 4).Value = "Categoria";
            worksheet.Cell(1, 4).Style.Font.Bold = true;
            worksheet.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 5).Value = "Cantidad";
            worksheet.Cell(1, 5).Style.Font.Bold = true;
            worksheet.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 6).Value = "Unidad";
            worksheet.Cell(1, 6).Style.Font.Bold = true;
            worksheet.Cell(1, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 7).Value = "Fecha Registro";
            worksheet.Cell(1, 7).Style.Font.Bold = true;
            worksheet.Cell(1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 8).Value = "Ubicacion";
            worksheet.Cell(1, 8).Style.Font.Bold = true;
            worksheet.Cell(1, 8).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Cell(1, 9).Value = "Estado";
            worksheet.Cell(1, 9).Style.Font.Bold = true;
            worksheet.Cell(1, 9).Style.Fill.BackgroundColor = XLColor.LightGray;
            //datos del cuerpo
            int row = 2;
            int renglon = 1;
            foreach (var item in data) 
            {
                worksheet.Cell(row, 1).Value = renglon;
                worksheet.Cell(row, 2).Value = item.Codebar;
                worksheet.Cell(row, 3).Value = item.ProductName;
                worksheet.Cell(row, 4).Value = item.Category;
                worksheet.Cell(row, 5).Value = item.Quantity;
                worksheet.Cell(row, 6).Value = item.Unidad;
                worksheet.Cell(row, 7).Value = item.DateScan;
                worksheet.Cell(row, 8).Value = item.Ubicacion;
                worksheet.Cell(row, 9).Value = item.Estado;
                row++;
                renglon++;
            }
            worksheet.Column(1).AdjustToContents();
            worksheet.Column(2).AdjustToContents();
            worksheet.Column(3).AdjustToContents();
            worksheet.Column(4).AdjustToContents();
            worksheet.Column(5).AdjustToContents();
            worksheet.Column(6).AdjustToContents();
            worksheet.Column(7).AdjustToContents();
            worksheet.Column(8).AdjustToContents();
            worksheet.Column(9).AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();
            //llamada JavaScript para descargar el excel.
            await JS.InvokeVoidAsync("downloadFileExcel", "datos.xlsx", Convert.ToBase64String(bytes));
        }
        public async Task<bool> UpdateScanProductsAsync(ScanProducts scanproduct)
        {
            var json = JsonSerializer.Serialize(scanproduct, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var url = $"api/orderfisico/updatedatascanproducts";
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public async Task GenerateReportsScanProducts(string OrderId)
        {
            var url = $"api/orderfisico/generatereportscanproducts/{OrderId}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            var base64 = Convert.ToBase64String(pdfBytes);
            var urlJS = $"data:application/pdf;base64,{base64}";

            //await JS.InvokeVoidAsync("open", urlJS);
        }
        public async Task<bool> DeletescanProducts(Guid id)
        {
            var url = $"api/orderfisico/deletescanProducts/{id}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        public async Task<List<ScanProducts>> GetscanProducts(string OrderId) 
        {
            var url = $"api/orderfisico/getscanproducts/{OrderId}";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new List<ScanProducts>();
            var scanProducts = await JsonSerializer.DeserializeAsync<List<ScanProducts>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (scanProducts ?? new List<ScanProducts>());
        }
        public async Task<bool> SaveDataProductScanAsync(List<ScanProducts> products)
        {
            var json = JsonSerializer.Serialize(products, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var url = $"api/orderfisico/savedatascanproducts";
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<List<OrderFisicoHeader>> GetOrders()
        {
            var url = $"api/orderfisico/getorders";
            var clientHttp = httpFactory.CreateClient("ritrama");
            var response = await clientHttp.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return new List<OrderFisicoHeader>();
            var orders = await JsonSerializer.DeserializeAsync<List<OrderFisicoHeader>>(
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), jsonOptions);
            return (orders ?? new List<OrderFisicoHeader>());
        }
        public async Task<bool> CreateOrders(OrderFisicoHeader order)
        {
            //algunos valores por defecto de la orden.
            order.Order_Hour = DateTime.Now.ToString("HH:mm:ss");
            order.Notes = string.Empty;
            var equip = new Equipo() { Id = new Guid(), OrderNumber=order.OrderNumberID,DateCreated=DateTime.Now };
            order.Equipo = equip;
            order.Status = "OPEN";
            order.Status_Name = "DOCUMENTO DE INVENTARIO";
            //primero hay que serializar la orden a json.
            var json = JsonSerializer.Serialize(order, jsonOptions);
            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
            var clientHttp = httpFactory.CreateClient("ritrama");
            var url = $"api/orderfisico/createorder";
            var response = await clientHttp.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        public Task<bool> DeleteOrders(string OrderNumber)
        {
            throw new NotImplementedException();
        }
        public Task<OrderFisicoHeader> GetOrderById(string OrderNumber)
        {
            throw new NotImplementedException();
        }
        public Task<bool> UpdateOrders(string OrderNumber, OrderFisicoHeader order)
        {
            throw new NotImplementedException();
        }

      
    }
}
