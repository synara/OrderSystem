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

            if (order == null) return;
            else
            {

                var orderId = order.ClOrdID.Value;
                var symbol = order.Symbol.Value;
                var side = order.Side.Value;
                var quantity = order.OrderQty.Value;
                var price = order.Price.Value;

                decimal impact = price * quantity * (side == Side.BUY ? 1 : -1);
                decimal currentExposure = exposureBySymbol.GetOrAdd(symbol, 0);
                decimal newExposure = currentExposure + impact;

                var execType = new ExecType(
                    Math.Abs(newExposure) > EXPOSURE_LIMIT
                        ? ExecType.REJECTED
                        : ExecType.NEW
                );

                var ordStatus = execType.Value.Equals(ExecType.REJECTED) ? OrdStatus.REJECTED : OrdStatus.NEW;

                var execReport = new ExecutionReport(
                    new OrderID(orderId),
                    new ExecID(Guid.NewGuid().ToString()),
                    execType,
                    new OrdStatus(ordStatus),
                    new Symbol(symbol),
                    new Side(side),
                    new LeavesQty(quantity),
                    new CumQty(0),
                    new AvgPx(price));

                execReport.Set(new ClOrdID(orderId));

                if (execType.Value.Equals(ExecType.NEW))
                    exposureBySymbol[symbol] = newExposure;

                try
                {
                    Session.SendToTarget(execReport, sessionID);
                    Console.WriteLine($"ExecutionReport enviado para {sessionID}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao enviar ExecutionReport: {ex}");
                }
            }

        }

        public void ToAdmin(QuickFix.Message message, SessionID sessionId)
        {
            Console.WriteLine("Mensagem administrativa recebida: " + message);
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionId)
        {
            Console.WriteLine("Mensagem administrativa recebida: " + message);
        }

        public void ToApp(QuickFix.Message message, SessionID sessionId)
        {
            Console.WriteLine("ToApp: " + message);
        }

        public void FromApp(QuickFix.Message message, SessionID sessionId)
        {
            Console.WriteLine($"Mensagem recebida: {message}");
            try
            {
                Crack(message, sessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Crack da mensagem: {ex}");
            }
        }

        public void OnCreate(SessionID sessionId)
        {
            Console.WriteLine($"Sessão criada: {sessionId}");
        }

        public void OnLogout(SessionID sessionId)
        {
            Console.WriteLine("Logout: " + sessionId);
        }

        public void OnLogon(SessionID sessionId)
        {
            Console.WriteLine("Logon: " + sessionId);
        }
    }
}
