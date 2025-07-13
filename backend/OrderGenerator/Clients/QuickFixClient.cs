using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System.Drawing;


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
            var execType = new ExecType(
               Math.Abs(5) > 6
                   ? ExecType.REJECTED
                   : ExecType.NEW
           );

            var execReport = new ExecutionReport(
                new OrderID(Guid.NewGuid().ToString()),
                new ExecID(Guid.NewGuid().ToString()),
                execType,
                new OrdStatus(OrdStatus.NEW),
            new Symbol("VALE3"),
                new Side(Side.SELL),
                new LeavesQty(3),
                new CumQty(0),
                new AvgPx(3)
            );

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

            Session.SendToTarget(order, sessionID);

            return false;
        }
    }
}
