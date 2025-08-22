namespace OrderService.DTOs
{
    public class CreateOrderDTO
    {
        public int CustomerId { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }
    }
}
