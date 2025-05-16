using Dto = WMS.Shared.Models.Documents;

namespace WMS.Backend.WebApi.Hubs
{
    public interface IOrderInClient
    {
        Task ReceiveOrderIn(Dto.OrderIn orderIn);
    }
}
