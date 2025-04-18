namespace WMS.Shared.Models.Documents
{
    public class OrderIn : Document
    {
        public List<OrderInProduct>? Products { get; set; }

        public class OrderInProduct
        {
            public Guid ProductId { get; set; }
            public double Count { get; set; }
        }
    }
}
