namespace WMS.Shared.Models.Documents
{
    public class OrderOut : Document
    {
        public List<OrderInProducts>? Products { get; set; }
    }
}
