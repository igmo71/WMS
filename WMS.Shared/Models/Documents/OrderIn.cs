namespace WMS.Shared.Models.Documents
{
    public class OrderIn : Document
    {
        public List<OrderInProducts>? Products { get; set; }
    }
}
