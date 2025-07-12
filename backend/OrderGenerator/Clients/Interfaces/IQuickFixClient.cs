using OrderGenerator.DTO;

namespace OrderGenerator.Clients.Interfaces
{
    public interface IQuickFixClient
    {
        bool NewOrder(OrderDto order);
    }
}
