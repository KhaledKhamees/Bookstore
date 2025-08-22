using OrderService.Models;

namespace OrderService.Services.Catalog
{
    public interface IBookCatalogClient
    {
        Task<BookSummary?> GetBookAsync(int bookId);
    }
}
