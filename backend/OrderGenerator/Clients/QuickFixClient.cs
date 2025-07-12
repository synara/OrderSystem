using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;


namespace OrderGenerator.Clients
{
    public class QuickFixClient : IQuickFixClient
    {
        private readonly SessionID sessionID;

        public QuickFixClient(SessionID sessionID)
        {
            this.sessionID = sessionID;
        }

        public bool NewOrder(OrderDto newOrder)
        {
            var order = new NewOrderSingle(
            new ClOrdID(Guid.NewGuid().ToString()),
            new Symbol(newOrder.Symbol),
            newOrder.Side.ToUpper() == "venda" ? new Side(Side.SELL) : new Side(Side.BUY),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));

            order.Set(new OrderQty(newOrder.Quantity));
            order.Set(new Price(newOrder.Price));

            return Session.SendToTarget(order, sessionID);
        }
    }
}
