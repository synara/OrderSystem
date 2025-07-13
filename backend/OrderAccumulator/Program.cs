using System;
using System.Threading;
using OrderAccumulator.Services;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

class Program
{
    private static ThreadedSocketAcceptor _acceptor;
    private static readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);

    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Iniciando OrderAccumulator FIX Server...");

            var settings = new SessionSettings(@"FIX/accumulator.cfg");
            var myApp = new QuickFixOrderService();
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);

            var logFactory2 = new ScreenLogFactory(settings);

            _acceptor = new ThreadedSocketAcceptor(myApp, storeFactory, settings, logFactory2);

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\nRecebido sinal de desligamento...");
                Shutdown();
                e.Cancel = true; 
            };

            _acceptor.Start();
            Console.WriteLine("Servidor FIX iniciado com sucesso. Pressione CTRL+C para encerrar.");

            _shutdownEvent.WaitOne();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro fatal: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static void Shutdown()
    {
        Console.WriteLine("Parando servidor FIX...");
        _acceptor?.Stop();
        Console.WriteLine("Servidor parado com sucesso.");
        _shutdownEvent.Set();
    }
}