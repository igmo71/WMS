namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderOut : Document
    {
        public List<OrderOutProduct>? Products { get; set; }
    }
}
