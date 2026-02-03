
namespace Shared.Dtos
{
    public class Equipo
    {

        public Guid Id { get; set; } = new Guid();
        public string OrderNumber { get; set; } = string.Empty;
        public string Persons { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public List<OrderFisicoDetails> products { get; set; } = new List<OrderFisicoDetails>();
        public DateTime DateCreated { get; set; }
    }
}
