using System.Text.Json.Serialization;

namespace OrderService.Models
{
    public class BookSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        [JsonPropertyName("stock")]
        public int Stock { get; set; }
    }
}
