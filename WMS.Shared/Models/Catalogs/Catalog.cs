using System.ComponentModel.DataAnnotations;

namespace WMS.Shared.Models.Catalogs
{
    public abstract class Catalog : EntityBase
    {
        [MinLength(3)]
        [MaxLength(100)]
        public string? Name { get; set; }
    }
}
