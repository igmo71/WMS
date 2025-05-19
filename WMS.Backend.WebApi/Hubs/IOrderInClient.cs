using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Hubs
{
    public interface IOrderInClient
    {
        Task OrderInCreated(Dto.OrderIn orderIn);
        Task OrderInUpdated(Dto.OrderIn orderIn);
        Task OrderInDeleted(Guid orderInId);
    }
}
