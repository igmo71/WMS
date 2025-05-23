namespace WMS.Backend.Domain.Models.Catalogs
{
    public enum StorageLocationStatus
    {
        Unknown = 0,
        Available, // Свободна
        Busy, // Занята
        Blocked // Заблокирована
    }
}