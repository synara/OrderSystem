using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Drawing;
using System.Threading.Tasks;


namespace OrderGenerator.Clients
{
    public class QuickFixClient : IQuickFixClient
    {
        private readonly SessionID sessionID;

        public QuickFixClient(SessionID sessionID)
        {
            this.sessionID = sessionID;
        }

        public async Task<OrderResultDto> NewOrder(OrderDto newOrder)
        {   
            var sessionID = new SessionID("FIX.4.4", "GENERATOR", "ACCUMULATOR");

            NewOrderSingle order = new NewOrderSingle
                (
                    new ClOrdID(Guid.NewGuid().ToString()),
                    new Symbol(newOrder.Symbol),
                    new Side(newOrder.Side.ToUpper().Equals("COMPRA") ? Side.BUY : Side.SELL),
                    new TransactTime(DateTime.Now),
                    new OrdType(OrdType.LIMIT)
                );

            order.Set(new OrderQty(newOrder.Quantity));
            order.Set(new Price(newOrder.Price));

            return new OrderResultDto().Create(Session.SendToTarget(order, sessionID), order.ClOrdID.Obj.ToString());
        }
    }
}
