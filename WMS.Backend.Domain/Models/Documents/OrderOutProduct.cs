namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderOutProduct
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public double Count { get; set; }
    }
}
