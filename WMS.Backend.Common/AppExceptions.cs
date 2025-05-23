namespace WMS.Backend.Common
{
    public class NotFoundException(string entityType, Guid id) : Exception($"{entityType} Not Found by {id}")
    { }
}
