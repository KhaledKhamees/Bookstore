namespace OrderService.Contracts
{
    public record OrderItemDTO(int BookId, int Quantity, decimal UnitPrice);
    public class OrderPlacedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalPrice { get; set; }
        public IReadOnlyList<OrderItemDTO> Items { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
