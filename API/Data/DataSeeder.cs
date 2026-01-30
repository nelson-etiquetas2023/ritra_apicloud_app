using Shared.Dtos;

namespace API.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context) 
        {
        //    if (context.Order_InvFisico_Header.Any())
        //    {
        //        var header_rows = context.Order_InvFisico_Header.ToList();
        //        foreach (var header in header_rows) 
        //        {
        //            context.Order_InvFisico_Header.Remove(header);
        //        }
        //        context.SaveChanges();
        //    }

            var orders = new List<OrderFisicoHeader>
            {
                new OrderFisicoHeader
                {
                    OrderNumberID = "ON-1024",
                    Order_Date = DateTime.Now.Date,
                    Order_Hour = DateTime.Now.Hour.ToString(),
                    Notes ="notes 1",
                    Person_Create = "nelson pino",
                    Status = false,
                    Status_Name ="document creted...",
                    Items = 0,
                    Document_Anulado = false,
                    Area_Almacen = "alm ppal.",
                    Sincro_Document = false,
                    OrdersDetails = new List<OrderFisicoDetails>
                    {
                        new OrderFisicoDetails { OrderNumberID="ON-1024", Product_id=10500, Product_name="papel coated general 880",
                            Product_Type="master",Renglon_Id=1,Roll_Id="1095041114",Code_Unique="10250",Width_Fisico=19,
                            Length_Fisico=19000,Width_Sistema=19,Length_Sistema=19000,Width_Dif=18, Length_Dif=5000,
                            Product_Estado="Ok",Ubicacion="m2-500",Notes="producto premium" },
                        new OrderFisicoDetails { OrderNumberID="ON-1024",Product_id=10499, Product_name="papel termico green",
                            Product_Type="master",Renglon_Id=2,Roll_Id="71180401122",Code_Unique="10251",Width_Fisico=18,
                            Length_Fisico=12000,Width_Sistema=18,Length_Sistema=2000,Width_Dif=18, Length_Dif=2000,
                            Product_Estado="Ok",Ubicacion="m2-501",Notes="producto no sale en el sistema" }
                    }
                },
                new OrderFisicoHeader
                {
                    OrderNumberID = "ON-1025",
                    Order_Date = DateTime.Now.Date,
                    Order_Hour = DateTime.Now.Hour.ToString(),
                    Notes ="notes 2",
                    Person_Create = "jose perez",
                    Status = false,
                    Status_Name ="document creted...",
                    Items = 2,
                    Document_Anulado = false,
                    Area_Almacen = "alm ppal.",
                    Sincro_Document = false,
                    OrdersDetails = new List<OrderFisicoDetails>
                    {
                        new OrderFisicoDetails { OrderNumberID = "ON-1025",Product_id=11002, Product_name="papel parafinado 120",
                            Product_Type="master",Renglon_Id=1,Roll_Id="21435041",Code_Unique="880",Width_Fisico=22,
                            Length_Fisico=21000,Width_Sistema=22,Length_Sistema=100,Width_Dif=22, Length_Dif=20500,
                            Product_Estado="Ok",Ubicacion="m1-700",Notes="producto en la estado" },
                        new OrderFisicoDetails { OrderNumberID = "ON-1025",Product_id=90111, Product_name="green peace ultimate 500",
                            Product_Type="master",Renglon_Id=2,Roll_Id="21435042",Code_Unique="881",Width_Fisico=39,
                            Length_Fisico=24000,Width_Sistema=39,Length_Sistema=500,Width_Dif=39, Length_Dif=23500,
                            Product_Estado="Ok",Ubicacion="m2-501",Notes="producto manchado" }
                    }
                }

            };
            context.Order_InvFisico_Header.AddRange(orders);
            context.SaveChanges();
        }
    }
}
