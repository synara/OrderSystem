using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace OrderGenerator.Clients
{
    public class QuickFixClient : MessageCracker, IQuickFixClient, IApplication
    {
        private SessionID sessionID;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResultDto>> pendingOrders = new();
        public void OnMessage(ExecutionReport report, SessionID sessionID)
        {
            var orderId = report.ClOrdID.getValue();
            var execType = report.ExecType.getValue();

            Console.WriteLine($"[OnMessage] Instance HashCode: {this.GetHashCode()}");

            var orderResult = new OrderResultDto().Create(execType != ExecType.REJECTED, orderId);

            if (pendingOrders.TryRemove(orderId, out var tcs)) tcs.SetResult(orderResult);
            else Console.WriteLine($"Sem pendências para a order {orderId}.");
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

            Console.WriteLine($"[NewOrder] Instance HashCode: {this.GetHashCode()}");

            var delay = Task.Delay(10000);
            var completed = await Task.WhenAny(tcs.Task, delay);

            Console.WriteLine($"TCS Task Id: {tcs.Task.Id}");
            Console.WriteLine($"Completed Task Id: {completed.Id}");
            Console.WriteLine("Completed == tcs.Task? " + (completed == tcs.Task));
            Console.WriteLine("Completed.Equals(tcs.Task)? " + completed.Equals(tcs.Task));

            if (!Session.SendToTarget(order, sessionID))
            {
                pendingOrders.TryRemove(orderId, out _);
                throw new Exception("Erro ao enviar ordem FIX. Revise os valores e a sessão.");
            }

            using var cts = new CancellationTokenSource(10000);
            using (cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
            {
                try
                {
                    return await tcs.Task;
                }
                catch (TaskCanceledException)
                {
                    pendingOrders.TryRemove(orderId, out _);
                    throw new TimeoutException("Sem resposta do servidor.");
                }
            }
        }

        #region IApplication methods

        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine($"Message received in FromApp:\n{message}");
            Console.WriteLine("Tipo da mensagem: " + message.GetType().Name);
            Console.WriteLine("MsgType: " + message.Header.GetString(Tags.MsgType));

            if (message.Header.GetString(Tags.MsgType) == MsgType.EXECUTION_REPORT)
            {
                var execReport = new ExecutionReport();
                execReport.FromString(message.ToString(), true, null, null);
                OnMessage(execReport, sessionID);
            }
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID) { }

        public void OnCreate(SessionID sessionID) { }

        public void OnLogon(SessionID sessionID)
        {
            this.sessionID = sessionID;
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
