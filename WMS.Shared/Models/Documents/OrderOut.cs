﻿using WMS.Backend.Domain.Models.Documents;

namespace WMS.Shared.Models.Documents
{
    public class OrderOut : Document
    {
        public List<OrderOutProduct>? Products { get; set; }
    }
}
