namespace PaymentService.Contracts
{
    public record OrderedItemDTO(int BookId, int Quantity, decimal UnitPrice);
    public class OrderplacedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalPrice { get; set; }
        public IReadOnlyList<OrderItemDTO> Items { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
