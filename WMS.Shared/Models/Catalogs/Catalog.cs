﻿namespace WMS.Shared.Models.Catalogs
{
    public abstract class Catalog
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}
