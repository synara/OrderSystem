using QuickFix.Fields;
using System;

namespace OrderGenerator.DTO
{
    public class OrderDto
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

    }
}
