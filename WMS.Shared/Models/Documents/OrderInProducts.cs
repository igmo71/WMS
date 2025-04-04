using System.ComponentModel.DataAnnotations;

namespace WMS.Shared.Models.Documents
{
    public class OrderInProducts
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
    }
}
