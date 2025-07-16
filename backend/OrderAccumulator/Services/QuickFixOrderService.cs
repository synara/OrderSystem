using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Collections.Concurrent;

namespace OrderAccumulator.Services
{
    public class QuickFixOrderService : MessageCracker, IApplication
    {
        private readonly ConcurrentDictionary<string, decimal> exposureBySymbol = new();
        private const decimal EXPOSURE_LIMIT = 100000000m;

        public void OnMessage(NewOrderSingle order, SessionID sessionID)
        {

            var orderId = order.ClOrdID.getValue();
            var symbol = order.Symbol.getValue();
            var side = order.Side.getValue();
            var quantity = order.OrderQty.getValue();
            var price = order.Price.getValue();

            decimal newExposure = NewExposureCalculation(symbol, side, quantity, price);

            var rejected = Math.Abs(newExposure) > EXPOSURE_LIMIT;

            var execType = new ExecType(rejected ? ExecType.REJECTED : ExecType.NEW);

            var execReport = new ExecutionReport(
                new OrderID(orderId),
                new ExecID(Guid.NewGuid().ToString()),
                execType,
                new OrdStatus(execType.getValue().Equals(ExecType.REJECTED) ? OrdStatus.REJECTED : OrdStatus.NEW),
                new Symbol(symbol),
                new Side(side),
                new LeavesQty(quantity),
                new CumQty(0),
                new AvgPx(price));

            execReport.Set(new ClOrdID(orderId));

            try
            {
                Session.SendToTarget(execReport, sessionID);

                if (rejected) Console.WriteLine($"Ordem rejeitada. Exposição excedida para símbolo {symbol}.");
                else
                {
                    Console.WriteLine($"Ordem {orderId} aceita para símbolo {symbol}. ");
                    exposureBySymbol[symbol] = newExposure;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar ExecutionReport: {ex}");
            }
        }

        public decimal NewExposureCalculation(string symbol, char side, decimal quantity, decimal price)
        {
            decimal impact = price * quantity * (side == Side.BUY ? 1 : -1);

            decimal currentExposure = exposureBySymbol.GetOrAdd(symbol, 0);

            return currentExposure + impact;
        }

        #region IApplication methods
        public void ToAdmin(QuickFix.Message message, SessionID sessionId) { }

        public void FromAdmin(QuickFix.Message message, SessionID sessionId) { }

        public void ToApp(QuickFix.Message message, SessionID sessionId) { }

        public void FromApp(QuickFix.Message message, SessionID sessionId)
        {
            Console.WriteLine($"Message received:\n{message}");

            try
            {
                if(message.Header.GetString(Tags.MsgType) == MsgType.ORDER_SINGLE)
                    Crack(message, sessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in the message Crack:\n{ex}");
            }
        }

        public void OnCreate(SessionID sessionId) => Console.WriteLine($"OnCreate {sessionId}");

        public void OnLogout(SessionID sessionId) => Console.WriteLine($"Logout {sessionId}");

        public void OnLogon(SessionID sessionId) => Console.WriteLine($"Logon {sessionId}");
        #endregion
    }
}
