using OrderService.Models;
using System.Net;

namespace OrderService.Services.Catalog
{
    public class BookCatalogClient : IBookCatalogClient
    {
        private readonly HttpClient _httpClient;
        public BookCatalogClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (_httpClient.Timeout == Timeout.InfiniteTimeSpan)
                _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }
        public async Task<BookSummary?> GetBookAsync(int bookId)
        {
            HttpResponseMessage response = _httpClient.GetAsync($"/api/Books/{bookId}").GetAwaiter().GetResult();
            if (response.StatusCode== HttpStatusCode.NotFound)
            {
                return null;
            }
            var book = response.Content.ReadFromJsonAsync<BookSummary>().GetAwaiter().GetResult();
            return book;
        }
    }
}
