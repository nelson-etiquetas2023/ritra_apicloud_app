using Shared.Dtos;

namespace API.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context) 
        {
            context.Parametros.RemoveRange(context.Parametros);
            context.SaveChanges();

            var documentSetting = new Parameter
            {
                   Name="Configuracion del consecutivo de inventario",
                   Module="DocumentSetting",
                   Value1="10000",
                   Value2 = "DOC",
                   Value3 = "true",
                   Value4 = "true",
                   Value5 = "#",
                   Value6 = "",
                   Value7 = "",
                   Value8 = "",
                   Value9 = "",
                   Value10 = ""
            };
            context.Parametros.Add(documentSetting);
            context.SaveChanges();
        }
    }
}
