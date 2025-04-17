using System.Text.Json.Serialization;
using WMS.Backend.Domain.Models.Catalogs;

namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderInProduct
    {
        public Guid OrderId { get; set; }

        [JsonIgnore]
        public OrderIn? Order { get; set; }


        public Guid ProductId { get; set; }

        [JsonIgnore]
        public Product? Product { get; set; }


        public double Count { get; set; }
    }
}
