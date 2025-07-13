using OrderGenerator.DTO;
using System.Threading.Tasks;

namespace OrderGenerator.Clients.Interfaces
{
    public interface IQuickFixClient
    {
        Task<OrderResultDto> NewOrder(OrderDto order);
    }
}
