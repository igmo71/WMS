namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderIn : Document
    {
        public List<OrderInProducts>? Products { get; set; }
    }
}
