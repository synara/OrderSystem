namespace OrderGenerator.DTO
{
    public class OrderDto
    {
        public string Symbol { get; set; } // PETR4, VALE3 ou VIIA4
        public string Side { get; set; } // Compra ou Venda
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
