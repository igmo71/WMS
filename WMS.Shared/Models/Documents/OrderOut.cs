namespace WMS.Shared.Models.Documents
{
    public class OrderOut : Document
    {
        public List<OrderOutProduct>? Products { get; set; }
    }
}
