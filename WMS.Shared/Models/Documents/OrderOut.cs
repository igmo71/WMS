namespace WMS.Shared.Models.Documents
{
    public class OrderOut : Document
    {
        public List<OrderOutProduct>? Products { get; set; }

        public class OrderOutProduct
        {
            public Guid ProductId { get; set; }
            public double Count { get; set; }
        }
    }
}
