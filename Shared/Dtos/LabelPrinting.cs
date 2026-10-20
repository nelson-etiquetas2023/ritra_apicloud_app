using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public static class LabelCategories
    {
        public const string Producto = "Producto";
        public const string Ubicacion = "Ubicacion";
        public const string SSCC = "SSCC";
        public const string Recepcion = "Recepcion";

        public static readonly string[] All = [Producto, Ubicacion, SSCC, Recepcion];
    }

    public static class PrintJobStatus
    {
        public const string Pending = "Pending";
        public const string Printing = "Printing";
        public const string Done = "Done";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }

    public class LabelTemplate
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        public string Codigo { get; set; } = ""; // LBL-0001

        [Required(ErrorMessage = "la plantilla debe tener nombre...")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [StringLength(20)]
        public string Categoria { get; set; } = LabelCategories.Producto;

        public double WidthIn { get; set; } = 2; // pulgadas
        public double HeightIn { get; set; } = 1; // pulgadas
        public int Dpi { get; set; } = 203;

        public string DesignJson { get; set; } = ""; // LabelDesign (diseñador Konva, fase 2)
        public string ZplTemplate { get; set; } = ""; // ZPL con tokens {{Product_*}}
        public string? ThumbnailBase64 { get; set; } // data:image/png;base64,...

        public int? PrinterId { get; set; } // impresora por defecto
        public int? EnterpriseId { get; set; } // empresa del encabezado (null = primera)
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class PrinterConnection
    {
        public const string Tcp = "TCP"; // red/WiFi: cola del servidor
        public const string Usb = "USB"; // directo navegador (Browser Print)
        public const string Serial = "SERIE"; // directo navegador
        public const string Parallel = "PARALELO"; // directo navegador
        public const string Bluetooth = "BLUETOOTH"; // directo navegador
        public const string Local = "LOCAL"; // Browser Print genérico

        public static readonly string[] All = [Tcp, Usb, Serial, Parallel, Bluetooth, Local];
        public static bool EsRed(string? c) => string.IsNullOrWhiteSpace(c) || c == Tcp;
    }

    public class Printer
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        public string Code { get; set; } = ""; // PRN-0001

        [Required(ErrorMessage = "la impresora debe tener nombre...")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(100)]
        public string Model { get; set; } = "";
        [StringLength(20)]
        public string Connection { get; set; } = PrinterConnection.Tcp;
        [StringLength(100)]
        public string Ip { get; set; } = "";
        public int Port { get; set; } = 9100;

        [StringLength(100)]
        public string Endpoint { get; set; } = ""; // COM1, LPT1, MAC, uid Browser Print
        public int Dpi { get; set; } = 203;
        public double AnchoMaxIn { get; set; } = 4;
        public bool IsActive { get; set; } = true;
    }

    public class PrintJob
    {
        [Key]
        public int Id { get; set; }

        public int TemplateId { get; set; }
        public int PrinterId { get; set; }
        public int? ProductId { get; set; }
        public string ProductSnapshotJson { get; set; } = "";
        public string ZplFinal { get; set; } = "";
        public int Copies { get; set; } = 1;

        [StringLength(20)]
        public string Status { get; set; } = PrintJobStatus.Pending;
        public int Orden { get; set; } // subir/bajar para priorizar

        [StringLength(100)]
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PrintedAt { get; set; }
        public string Error { get; set; } = "";
    }

    public class PrintJobEnqueueRequest
    {
        public int TemplateId { get; set; }
        public int PrinterId { get; set; }
        public int? ProductId { get; set; }
        public int? EnterpriseId { get; set; }
        public int Copies { get; set; } = 1;
        public string CreatedBy { get; set; } = "";
    }

    public class PrintJobReorderItem
    {
        public int Id { get; set; }
        public int Orden { get; set; }
    }

    public class PreviewZplRequest
    {
        public int? TemplateId { get; set; }
        public string Zpl { get; set; } = "";
        public int? ProductId { get; set; }
        public int Copies { get; set; } = 1;
    }

    public class LabelDesign
    {
        public double WidthIn { get; set; } = 2;
        public double HeightIn { get; set; } = 1;
        public int Dpi { get; set; } = 203;
        public List<LabelElement> Elements { get; set; } = [];
    }

    public class LabelElement
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "Text"; // Text|Barcode|Qr|Rect|Line|Image
        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }
        public string Value { get; set; } = "";
        public double FontSize { get; set; } = 12;
        public string BarcodeType { get; set; } = "CODE128"; // CODE128|QR
        public int Rotation { get; set; } // 0|90|180|270
        public string Align { get; set; } = "L";
        public string Src { get; set; } = "";
    }
}
