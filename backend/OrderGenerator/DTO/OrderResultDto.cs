namespace OrderGenerator.DTO
{
    public class OrderResultDto
    {
        public string OrderId { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }

        public OrderResultDto Create(bool accepted, string orderId)
        {
            return new OrderResultDto()
            {
                OrderId = orderId,
                Message = accepted ? "Ordem recebida e aceita." : "Ordem rejeitada."
            };
        }
    }
}
