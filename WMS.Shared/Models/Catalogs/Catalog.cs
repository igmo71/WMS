﻿using WMS.Backend.Domain.Models;

namespace WMS.Shared.Models.Catalogs
{
    public abstract class Catalog : EntityBase
    {
        public string? Name { get; set; }
    }
}
