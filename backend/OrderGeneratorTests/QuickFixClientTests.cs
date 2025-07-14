using Xunit;
using Moq;
using OrderGenerator.Clients.Interfaces;
using OrderGenerator.DTO;
using System.Threading.Tasks;

namespace OrderGeneratorTests;


public class QuickFixClientTests
{
    [Fact]
    public async Task ReturnsSuccessWhenOrderIsAccepted()
    {
        // Arrange
        var mockClient = new Mock<IQuickFixClient>();

        var inputOrder = new OrderDto
        {
            Symbol = "PETR4",
            Side = "Compra",
            Price = 20,
            Quantity = 10000
        };

        var expectedResult = new OrderResultDto
        {
            OrderId = "12345",
            Success = true
        };

        mockClient
            .Setup(c => c.NewOrder(It.IsAny<OrderDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockClient.Object.NewOrder(inputOrder);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("12345", result.OrderId);
    }

    [Fact]
    public async Task ThrowsTimeoutException_WhenServerDoesNotRespond()
    {
        var mockClient = new Mock<IQuickFixClient>();
        var order = new OrderDto
        {
            Symbol = "VALE3",
            Side = "Venda",
            Price = 100,
            Quantity = 1000
        };

        mockClient
            .Setup(c => c.NewOrder(It.IsAny<OrderDto>()))
            .ThrowsAsync(new TimeoutException("Sem resposta do servidor."));

        var ex = await Assert.ThrowsAsync<TimeoutException>(() => mockClient.Object.NewOrder(order));
        Assert.Equal("Sem resposta do servidor.", ex.Message);
    }
}
