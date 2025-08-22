using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services.Catalog;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderServiceContext _context;
        private readonly IBookCatalogClient _clint;
        private readonly ILogger<OrdersController> _log;
        public OrdersController(OrderServiceContext context,IBookCatalogClient client, ILogger<OrdersController> log)
        {
            _context = context;
            _clint = client;
            _log = log;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrder()
        {
            return await _context.Order.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Order.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(CreateOrderDTO order)
        {
            if (order.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }
            var book = await _clint.GetBookAsync(order.BookId);

            // 1) Validate book exists + get current price & stock
            if (book is null)
                return Problem(statusCode: 400, title: $"Book {order.BookId} not found.");

            // 2) Validate stock availability
            if (order.Quantity > book.Stock)
                return Problem(statusCode: 409, title: "Insufficient stock.",
                    detail: $"Requested {order.Quantity}, available {book.Stock}.");

            // 3) Compute total price server-side
            var totalPrice = book.Price * order.Quantity;

            var newOrder = new Order
            {
                CustomerId = order.CustomerId,
                BookId = order.BookId,
                Quantity = order.Quantity,
                TotalPrice = totalPrice,
                Status = "Pending"
            };
            _context.Order.Add(newOrder);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = newOrder.Id }, newOrder);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
