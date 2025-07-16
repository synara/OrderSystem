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
        private SessionID? sessionID;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResultDto>> pendingOrders = new();
        public void OnMessage(ExecutionReport report, SessionID sessionID)
        {
            var orderId = report.ClOrdID.getValue();
            var execType = report.ExecType.getValue();

            var orderResult = OrderResultDto.Create(execType != ExecType.REJECTED, orderId);

            if (pendingOrders.TryRemove(orderId, out var tcs)) tcs.SetResult(orderResult);
            else Console.WriteLine($"Sem pendências para a order {orderId}.");
        }

        public async Task<OrderResultDto> NewOrder(OrderDto newOrder)
        {
            Validations(newOrder);

            var orderId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<OrderResultDto>();
            pendingOrders[orderId] = tcs;

            var order = new NewOrderSingle(
                new ClOrdID(orderId),
                new Symbol(newOrder.Symbol),
                new Side(newOrder.Side.ToUpper() == "COMPRA" ? Side.BUY : Side.SELL),
                new TransactTime(DateTime.UtcNow),
                new OrdType(OrdType.LIMIT)
            );

            order.Set(new OrderQty(newOrder.Quantity));
            order.Set(new Price(newOrder.Price));

            bool sent = Session.SendToTarget(order, sessionID!);

            if (!sent)
            {
                pendingOrders.TryRemove(orderId, out _);
                throw new Exception("Erro ao enviar ordem FIX. Revise os valores e a sessão.");
            }

            var delay = Task.Delay(10000);
            var completed = await Task.WhenAny(tcs.Task, delay);

            if (completed == tcs.Task)
                return await tcs.Task;
            else
            {
                pendingOrders.TryRemove(orderId, out _);
                throw new TimeoutException("Sem resposta do servidor.");
            }
        }

        private void Validations(OrderDto newOrder)
        {
            if (newOrder.Quantity <= 0 || newOrder.Quantity >= 100000)
                throw new Exception("Quantidade precisa ser menor que 100.000.");
            if (newOrder.Price <= 0 || newOrder.Price >= 1000)
                throw new Exception("Preço precisa ser menor que R$1.000.");

            if (!(newOrder.Price % 0.01m).Equals(0))
                throw new Exception("Preço precisa ser múltiplo de 0.01.");
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
