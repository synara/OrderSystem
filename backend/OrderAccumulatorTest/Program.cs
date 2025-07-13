using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        var settings = new SessionSettings("accumulator.cfg");
        var app = new ExecutionReportTester();
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new ScreenLogFactory(settings);
        var acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutting down...");
            acceptor.Stop();
            e.Cancel = true;
        };

        acceptor.Start();
        Console.WriteLine("FIX Acceptor started. Waiting for connections... Press Ctrl+C to exit.");

        Thread.Sleep(Timeout.Infinite);
    }
}

class ExecutionReportTester : MessageCracker, IApplication
{
    public void FromAdmin(QuickFix.Message message, SessionID sessionID) => Console.WriteLine($"[FromAdmin] {message}");
    public void ToAdmin(QuickFix.Message message, SessionID sessionID) => Console.WriteLine($"[ToAdmin] {message}");
    public void OnCreate(SessionID sessionID) => Console.WriteLine($"[Session Created] {sessionID}");
    public void OnLogon(SessionID sessionID) => Console.WriteLine($"[Logon] {sessionID}");
    public void OnLogout(SessionID sessionID) => Console.WriteLine($"[Logout] {sessionID}");

    public void ToApp(QuickFix.Message message, SessionID sessionID)
    {
        Console.WriteLine($"[ToApp] {message}");
    }

    public void FromApp(QuickFix.Message message, SessionID sessionID)
    {
        Console.WriteLine($"[FromApp] {message}");
        try
        {
            Crack(message, sessionID);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Crack ERROR] {ex}");
        }
    }

    public void OnMessage(NewOrderSingle order, SessionID sessionID)
    {
        Console.WriteLine($"[Received Order] {order}");

        try
        {
            var execReport = new ExecutionReport(
                new OrderID(Guid.NewGuid().ToString()),
                new ExecID(Guid.NewGuid().ToString()),
                new ExecType(ExecType.NEW),
                new OrdStatus(OrdStatus.NEW),
                new Symbol("PETR4"),
                new Side(Side.BUY),
                new LeavesQty(100),
                new CumQty(0),
                new AvgPx(50.55m)
            );

            execReport.Set(new ClOrdID(order.ClOrdID.Obj));
            Console.WriteLine("[ExecutionReport created successfully]");
            Console.WriteLine(execReport.ToString());

            Session.SendToTarget(execReport, sessionID);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExecutionReport ERROR] {ex}");
        }
    }
}
