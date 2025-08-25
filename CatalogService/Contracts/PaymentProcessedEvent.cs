namespace CatalogService.Contracts
{
    public record OrderItemDTO(int BookId, int Quantity, decimal UnitPrice);

    public class PaymentProcessedEvent
    {
            public int OrderId { get; set; }
            public IReadOnlyList<OrderItemDTO> Items { get; set; }   
            public DateTime ProcessedAtUtc { get; set; }
    }
}
