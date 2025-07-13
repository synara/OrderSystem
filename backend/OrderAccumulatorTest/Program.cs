using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Store;
using QuickFix.Transport;
using QuickFix.Logger;
using System;
using System.Threading;

class Program
{
    private static ThreadedSocketAcceptor? _acceptor;
    private static ManualResetEventSlim _shutdownEvent = new(false);

    static void Main()
    {
        var settings = new SessionSettings("accumulatorteste.cfg");
        var app = new SimpleFixApp();
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new ScreenLogFactory(settings);

        _acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Stopping FIX server...");
            _acceptor.Stop();
            _shutdownEvent.Set();
            e.Cancel = true;
        };

        Console.WriteLine("Starting FIX server...");
        _acceptor.Start();

        Console.WriteLine("FIX server started. Press Ctrl+C to stop.");
        _shutdownEvent.Wait();
        Console.WriteLine("FIX server stopped.");
    }
}

class SimpleFixApp : MessageCracker, IApplication
{
    public void FromAdmin(QuickFix.Message message, SessionID sessionID) => Console.WriteLine($"FromAdmin: {message}");

    public void FromApp(QuickFix.Message message, SessionID sessionID)
    {
        Console.WriteLine($"FromApp: {message}");
        try
        {
            Crack(message, sessionID);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in Crack: {ex}");
        }
    }

    public void OnCreate(SessionID sessionID) => Console.WriteLine($"Session created: {sessionID}");

    public void OnLogon(SessionID sessionID) => Console.WriteLine($"Logon: {sessionID}");

    public void OnLogout(SessionID sessionID) => Console.WriteLine($"Logout: {sessionID}");

    public void ToAdmin(QuickFix.Message message, SessionID sessionID) => Console.WriteLine($"ToAdmin: {message}");

    public void ToApp(QuickFix.Message message, SessionID sessionID) => Console.WriteLine($"ToApp: {message}");

    public void OnMessage(NewOrderSingle order, SessionID sessionID)
    {
        Console.WriteLine($"Received NewOrderSingle: ClOrdID={order.ClOrdID}, Symbol={order.Symbol}, Side={order.Side}, Qty={order.OrderQty}, Price={order.Price}");

        var execReport = new ExecutionReport(
            new OrderID(Guid.NewGuid().ToString()),
            new ExecID(Guid.NewGuid().ToString()),
            new ExecType(ExecType.NEW),
            new OrdStatus(OrdStatus.NEW),
            order.Symbol,
            order.Side,
            new LeavesQty(order.OrderQty.Value),
            new CumQty(0),
            new AvgPx(order.Price.Value)
        );

        // Normalmente você enviaria a mensagem, mas aqui só logamos para teste
        Console.WriteLine("ExecutionReport criado com sucesso:");
        Console.WriteLine(execReport);
    }
}
