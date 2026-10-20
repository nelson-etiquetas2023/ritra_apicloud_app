using System.Text.Json;
using System.Text.RegularExpressions;
using Shared.Dtos;

namespace API.Services.PrintJobs
{
    public static partial class LabelGenerator
    {
        [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}")]
        private static partial Regex TokenRegex();

        [GeneratedRegex(@"\^PQ[^^]*")]
        private static partial Regex PqRegex();

        public static Dictionary<string, string> ProductValues(Product p)
        {
            var codebar = string.IsNullOrWhiteSpace(p.Codebar) ? p.Product_Code : p.Codebar;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Product_Id"] = p.Product_id.ToString(),
                ["Product_Code"] = p.Product_Code ?? "",
                ["Product_Name"] = p.Product_Name ?? "",
                ["Product_Type"] = p.Product_Type ?? "",
                ["Price"] = p.Price.ToString("0.##"),
                ["Codebar"] = codebar ?? "",
                ["Unidad"] = p.Unidad ?? "",
                ["Costo"] = p.Costo.ToString("0.##"),
                ["Stock"] = p.Stock.ToString("0.##"),
                ["SkuNumber"] = p.SkuNumber ?? "",
                ["PartNumber"] = p.PartNumber ?? "",
                ["Model"] = p.Model ?? "",
                ["Marca"] = p.Marca ?? "",
                ["Description"] = p.Description ?? "",
                ["FechaVencimiento"] = p.ManejaVencimiento && p.FechaVencimiento.HasValue
                    ? p.FechaVencimiento.Value.ToString("dd/MM/yyyy")
                    : ""
            };
        }

        public static Dictionary<string, string> EnterpriseValues(Enterprise e) =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Enterprise_Id"] = e.enterprise_id.ToString(),
                ["Enterprise_Name"] = e.Nombre_Empresa ?? "",
                ["Enterprise_Code"] = e.Codigo_Empresa ?? "",
                ["Enterprise_RNC"] = e.Registro_Fiscal ?? "",
                ["Enterprise_Address"] = e.Direccion ?? "",
                ["Enterprise_Phone"] = e.Telefono ?? ""
            };

        public static Dictionary<string, string> SampleValues() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Product_Id"] = "0",
            ["Product_Code"] = "MUESTRA-001",
            ["Product_Name"] = "Producto de muestra",
            ["Product_Type"] = "Varios",
            ["Price"] = "0",
            ["Codebar"] = "MUESTRA-001",
            ["Unidad"] = "pieza",
            ["Costo"] = "0",
            ["Stock"] = "0",
            ["SkuNumber"] = "",
            ["PartNumber"] = "",
            ["Model"] = "",
            ["Marca"] = "",
            ["Description"] = "",
            ["FechaVencimiento"] = "31/12/2026",
            ["Enterprise_Id"] = "0",
            ["Enterprise_Name"] = "Mi empresa",
            ["Enterprise_Code"] = "",
            ["Enterprise_RNC"] = "",
            ["Enterprise_Address"] = "",
            ["Enterprise_Phone"] = ""
        };

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("^", "").Replace("~", "").Replace("_", " ").Trim();
        }

        public static (string Zpl, List<string> Missing) MergeTokens(string template, Dictionary<string, string> values)
        {
            var missing = new List<string>();
            var result = TokenRegex().Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                if (values.TryGetValue(key, out var v)) return Sanitize(v);
                if (!missing.Contains(key)) missing.Add(key);
                return m.Value;
            });
            return (result, missing);
        }

        public static string ApplyCopies(string zpl, int copies)
        {
            if (copies < 1) copies = 1;
            // Quita ^PQ aunque comparta línea con ^XZ u otros comandos.
            var noPq = PqRegex().Replace(zpl, "");
            var pq = $"^PQ{copies},0,1,Y";
            var idx = noPq.LastIndexOf("^XZ", StringComparison.Ordinal);
            if (idx >= 0) return noPq.Insert(idx, pq + "\n");
            return noPq.TrimEnd() + "\n" + pq + "\n^XZ";
        }

        public static string BuildZpl(LabelTemplate template, Dictionary<string, string> values, int copies)
        {
            if (string.IsNullOrWhiteSpace(template.ZplTemplate))
                throw new InvalidOperationException(
                    "La plantilla no tiene ZPL. Importe el .zpl de ZPLDesigner (el diseñador Konva aún no genera ZPL).");

            var (merged, missing) = MergeTokens(template.ZplTemplate, values);
            if (missing.Count > 0)
                throw new InvalidOperationException($"Tokens sin valor: {string.Join(", ", missing)}.");

            var body = merged.Trim();
            if (!body.Contains("^XA"))
            {
                var pw = (int)Math.Round(template.WidthIn * template.Dpi);
                var ll = (int)Math.Round(template.HeightIn * template.Dpi);
                body = $"^XA\n^PW{pw}^LL{ll}\n{body}\n^XZ";
            }

            return ApplyCopies(body, copies);
        }

        public static string Snapshot(Product p) =>
            JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = false });
    }
}
