using OrderAccumulator.Services;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System.Reflection;
using Moq;
using System.Collections.Concurrent;


namespace OrderAccumulatorTests;

public class QuickFixOrderServiceTests
{
    SessionID sessionId = new("FIX.4.4", "GENERATOR", "ACCUMULATOR");
    QuickFixOrderService service = new();

    private NewOrderSingle NewOrder(string orderId, string symbol, char side, decimal qty, decimal price)
    {
        var order = new NewOrderSingle(
            new ClOrdID(orderId),
            new Symbol(symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT)
        );

        order.Set(new OrderQty(qty));
        order.Set(new Price(price));

        return order;
    }

    [Fact]
    public void OrderShouldBeRejectForExceedLimit()
    {
        service.OnMessage(NewOrder("RejectedByLimit-12345", "VIIA4", Side.BUY, 500000, 300), sessionId);
    }

    [Fact]
    public void OrderShouldBeAccepted()
    {   
        service.OnMessage(NewOrder("Accepted-56789", "PETR4", Side.BUY, 10000, 55), sessionId);
    }

    [Fact]
    public void ShouldCalculateCorrectSaleExposure()
    {
        var n = service.NewExposureCalculation("VALE3", Side.SELL, 3, 355.55m);
        Assert.Equal(-1066.65m, service.NewExposureCalculation("VALE3", Side.SELL, 3, 355.55m));
    }

    [Fact]
    public void ShouldCalculateCorrectSaBuyExposure()
    {
        var n = service.NewExposureCalculation("VALE3", Side.SELL, 3, 355.55m);
        Assert.Equal(1066.65m, service.NewExposureCalculation("VALE3", Side.BUY, 3, 355.55m));
    }
}