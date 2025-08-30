using OrderService.Models;
using System.Net;

namespace OrderService.Services.Catalog
{
    public class BookCatalogClient : IBookCatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookCatalogClient> _logger;
        public BookCatalogClient(HttpClient httpClient, ILogger<BookCatalogClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            if (_httpClient.Timeout == Timeout.InfiniteTimeSpan)
            {
                _logger.LogWarning("HttpClient timeout is set to infinite. Setting it to 5 seconds.");
                _httpClient.Timeout = TimeSpan.FromSeconds(5);
            }
                
        }
        public async Task<BookSummary?> GetBookAsync(int bookId)
        {
            _logger.LogInformation("Fetching book with ID {BookId} from Catalog Service", bookId);
            HttpResponseMessage response = _httpClient.GetAsync($"/api/Books/{bookId}").GetAwaiter().GetResult();
            if (response.StatusCode== HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Book with ID {BookId} not found in Catalog Service", bookId);
                return null;
            }
            var book = response.Content.ReadFromJsonAsync<BookSummary>().GetAwaiter().GetResult();
            _logger.LogInformation("Retrieved book with ID {BookId} from Catalog Service", bookId);
            return book;
        }
    }
}
