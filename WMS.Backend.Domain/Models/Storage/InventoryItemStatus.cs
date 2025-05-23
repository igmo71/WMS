namespace WMS.Backend.Domain.Models.Storage
{
    public enum InventoryItemStatus
    {
        Unknown = 0,
        Available, // Доступен
        Reserved, // Зарезервvирован
        Defective // Брак
    }
}
