using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OrderGenerator.Clients
{
    public class QuickFixClient : MessageCracker, IQuickFixClient, IApplication
    {
        private readonly SessionID sessionID;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResultDto>> pendingOrders = new();

        public QuickFixClient(SessionID sessionID)
        {
            this.sessionID = sessionID;
        }

        // Recebe ExecutionReport do OrderAccumulator
        public void OnMessage(ExecutionReport report, SessionID sessionID)
        {
            var orderId = report.ClOrdID.getValue();
            var execType = report.ExecType.getValue();

            var orderResult = new OrderResultDto().Create(execType != ExecType.REJECTED, orderId);

            if (pendingOrders.TryRemove(orderId, out var tcs)) tcs.SetResult(orderResult);
            else Console.WriteLine($"No pending order found for ClOrdID={orderId}");
        }

        public async Task<OrderResultDto> NewOrder(OrderDto newOrder)
        {
            var orderId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<OrderResultDto>();
            pendingOrders[orderId] = tcs; //taskscomplets

            var order = new NewOrderSingle(
                new ClOrdID(orderId),
                new Symbol(newOrder.Symbol),
                new Side(newOrder.Side.ToUpper() == "COMPRA" ? Side.BUY : Side.SELL),
                new TransactTime(DateTime.UtcNow),
                new OrdType(OrdType.LIMIT)
            );

            order.Set(new OrderQty(newOrder.Quantity));
            order.Set(new Price(newOrder.Price));

            bool sent = Session.SendToTarget(order, sessionID);

            if (!sent)
            {
                pendingOrders.TryRemove(orderId, out _);
                throw new Exception("Erro ao enviar ordem FIX. Revise os valores e a sessão.");
            }

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            if (completed == tcs.Task)
                return await tcs.Task;
            else
            {
                pendingOrders.TryRemove(orderId, out _);
                throw new TimeoutException("Sem resposta do servidor.");
            }
        }

        #region IApplication methods

        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine($"Message received in FromApp:\n{message}");
            try
            {
                if (message.Header.GetString(Tags.MsgType) == MsgType.EXECUTIONREPORT)

                    Crack(message, sessionID);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cracking message: {ex}");
            }
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }

        public void OnCreate(SessionID sessionID) { }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine($"Logon - Session: {sessionID}");
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine($"Logout - Session: {sessionID}");
        }

        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { }

        public void ToApp(QuickFix.Message message, SessionID sessionID) { }

        #endregion
    }
}
