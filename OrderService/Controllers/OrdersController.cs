using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Contracts;
using OrderService.Contracts;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services.Catalog;
using OrderService.Services.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderServiceContext _context;
        private readonly IBookCatalogClient _clint;
        private readonly ILogger<OrdersController> _logger;
        private readonly IRabbitMQProducer _rabbitMQProducer;


        public OrdersController(OrderServiceContext context,IBookCatalogClient client, ILogger<OrdersController> log, IRabbitMQProducer rabbitMQProducer)
        {
            _context = context;
            _clint = client;
            _logger = log;
            _rabbitMQProducer = rabbitMQProducer;
        }
        [Authorize(Roles = "Admin")]
        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrder()
        {
            _logger.LogInformation("Getting all orders");
            var orders = await _context.Order.ToListAsync();
            if (orders == null || orders.Count == 0)
            {
                _logger.LogWarning("No orders found");
                return NotFound("No orders found");
            }
            _logger.LogInformation("Retrieved {Count} orders", orders.Count);
            return orders;
        }
        [Authorize(Roles = "Admin")]
        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            _logger.LogInformation("Getting order with ID {Id}", id);
            var order = await _context.Order.FindAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("Retrieved order with ID {Id}", id);
            return order;
        }
        [Authorize(Roles = "Customer")]
        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            _logger.LogInformation("Updating order with ID {Id}", id);
            if (id != order.Id)
            {
                _logger.LogWarning("Order ID mismatch: {Id} != {OrderId}", id, order.Id);
                return BadRequest();
            }
            _logger.LogInformation("Marking order with ID {Id} as modified", id);
            _context.Entry(order).State = EntityState.Modified;

            try
            {
                _logger.LogInformation("Saving changes for order with ID {Id}", id);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    _logger.LogError("Order with ID {Id} not found during update", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogCritical("Concurrency exception occurred while updating order ID {OrderId}", id);
                    throw;
                }
            }

            return NoContent();
        }
        [Authorize(Roles = "Customer")]
        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(CreateOrderDTO order)
        {
            _logger.LogInformation("Creating a new order for Customer ID {CustomerId} , Book ID {BookId} and Quantity {Quantity}", order.CustomerId, order.BookId,order.Quantity);
            if (order.Quantity <= 0)
            {
                _logger.LogWarning("Invalid quantity {Quantity} for order", order.Quantity);
                return BadRequest("Quantity must be greater than zero.");
            }
            var book = await _clint.GetBookAsync(order.BookId);

            // 1) Validate book exists + get current price & stock
            if (book is null)
            {
                _logger.LogError("Book with ID {BookId} not found", order.BookId);
                return Problem(statusCode: 400, title: $"Book {order.BookId} not found.");
            }
                

            // 2) Validate stock availability
            if (order.Quantity > book.Stock)
            {
                _logger.LogError("Insufficient stock for Book ID {BookId}: Requested {Requested}, Available {Available}", order.BookId, order.Quantity, book.Stock);
                return Problem(statusCode: 409, title: "Insufficient stock.",
                    detail: $"Requested {order.Quantity}, available {book.Stock}.");
            }
                

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
            var OrderEvent = new OrderPlacedEvent()
            {
                OrderId = newOrder.Id,
                CustomerId = newOrder.CustomerId,
                TotalPrice = newOrder.TotalPrice,
                Items = new List<OrderItemDTO> { new OrderItemDTO(newOrder.BookId, newOrder.Quantity, book.Price) },
                CreatedAtUtc = DateTime.UtcNow
            };
            _logger.LogInformation("Order ID {OrderId} created successfully", newOrder.Id);
            _context.Order.Add(newOrder);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Publishing order event for Order ID {OrderId}", newOrder.Id);
            _rabbitMQProducer.SendProductMessage("OrderQueue", OrderEvent);

            return CreatedAtAction("GetOrder", new { id = newOrder.Id }, newOrder);
        }
        [Authorize(Roles = "Customer,Admin")]
        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            _logger.LogInformation("Deleting order ID {OrderId}", id);
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order ID {OrderId} not found for deletion", id);
                return NotFound();
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order ID {OrderId} deleted successfully", id);
            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
