using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly CatalogServiceContext _context;
        private readonly ILogger<BooksController> _logger;

        public BooksController(CatalogServiceContext context, ILogger<BooksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBook()
        {
            _logger.LogInformation("Fetching all books from the catalog.");
            var books = await _context.Book.ToListAsync();
            _logger.LogInformation("Fetched {Count} books from the catalog.", books.Count);
            return books;
        }

        // GET: api/Books/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            _logger.LogInformation("Fetching book with ID {BookId} from the catalog.", id);
            var book = await _context.Book.FindAsync(id);
            _logger.LogInformation("Fetched book: {@Book}", book);
            if (book == null)
            {
                _logger.LogWarning("Book with ID {BookId} not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Returning book with ID {BookId}.", id);
            return book;
        }
        [Authorize(Roles = "Admin")]
        // PUT: api/Books/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            _logger.LogInformation("Updating book with ID {BookId}.", id);
            if (id != book.Id)
            {
                _logger.LogWarning("Book ID in the URL does not match the book ID in the body.");
                return BadRequest();
            }
            _logger.LogInformation("Book details to update: {@Book}", book);
            _context.Entry(book).State = EntityState.Modified;

            try
            {
                _logger.LogInformation("Saving changes to the database.");
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(id))
                {
                    _logger.LogWarning("Book with ID {BookId} not found during update.", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error occurred while updating book with ID {BookId}.", id);
                    throw;
                }
            }

            return NoContent();
        }
        [Authorize(Roles ="Admin")]
        // POST: api/Books
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            _logger.LogInformation("Adding a new book to the catalog: {@Book}", book);
            _context.Book.Add(book);
            _logger.LogInformation("Saving changes to the database.");
            await _context.SaveChangesAsync();
            _logger.LogInformation("New book added with ID {BookId}.", book.Id);
            return CreatedAtAction("GetBook", new { id = book.Id }, book);
        }
        [Authorize(Roles = "Admin")]
        // DELETE: api/Books/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            _logger.LogInformation("Deleting book with ID {BookId}.", id);
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                _logger.LogWarning("Book with ID {BookId} not found for deletion.", id);
                return NotFound();
            }
            _logger.LogInformation("Book found: {@Book}. Proceeding with deletion.", book);
            _context.Book.Remove(book);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Book with ID {BookId} deleted successfully.", id);  

            return NoContent();
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.Id == id);
        }
    }
}
