namespace WMS.Backend.Domain.Models.Documents
{
    public class OrderIn : Document
    {
        public List<OrderInProduct>? Products { get; set; }
    }
}
