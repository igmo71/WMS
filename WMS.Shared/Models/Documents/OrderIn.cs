﻿using WMS.Backend.Domain.Models.Documents;

namespace WMS.Shared.Models.Documents
{
    public class OrderIn : Document
    {
        public List<OrderInProduct>? Products { get; set; }
    }
}
