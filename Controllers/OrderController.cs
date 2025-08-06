using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Services;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartOrderingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            Console.WriteLine($"Received POST /api/Order at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            Console.WriteLine($"Authorization Header: {Request.Headers["Authorization"]}");
            Console.WriteLine($"User: {User.Identity?.Name ?? "Unknown"}, IsAuthenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Roles: {string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value))}");
            Console.WriteLine($"All Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
            Console.WriteLine($"Request Body: {System.Text.Json.JsonSerializer.Serialize(orderRequest)}");

            if (!User.Identity.IsAuthenticated)
            {
                var errorResponse = new { error = "Unauthorized: Invalid or missing authentication token." };
                Console.WriteLine($"Returning 401: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return Unauthorized(errorResponse);
            }

            if (!ModelState.IsValid || orderRequest?.Order == null)
            {
                var errorResponse = new { error = "Invalid request: Order is missing or invalid.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) };
                Console.WriteLine($"Returning 400: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return BadRequest(errorResponse);
            }

            try
            {
                var order = new Order
                {
                    CustomerName = orderRequest.Order.CustomerName,
                    CustomerPhone = orderRequest.Order.CustomerPhone,
                    TotalPrice = orderRequest.Order.TotalPrice,
                    Status = orderRequest.Order.Status ?? "Received",
                    OrderDate = orderRequest.Order.OrderDate,
                    Items = orderRequest.Order.Items?.Select(i => new OrderItem
                    {
                        MenuItemId = i.MenuItemId,
                        Name = i.Name,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList() ?? new List<OrderItem>()
                };

                var createdOrder = await _orderService.CreateAsync(order);
                if (createdOrder == null)
                {
                    var errorResponse = new { error = "Failed to create order: Invalid response from service." };
                    Console.WriteLine($"Returning 400: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                    return BadRequest(errorResponse);
                }

                var response = new
                {
                    success = true,
                    orderId = createdOrder.Id,
                    customerName = createdOrder.CustomerName,
                    totalPrice = createdOrder.TotalPrice
                };
                Console.WriteLine($"Order created: ID={createdOrder.Id}, Customer={createdOrder.CustomerName}, Items={createdOrder.Items?.Count ?? 0}, TotalPrice={createdOrder.TotalPrice}");
                Console.WriteLine($"Returning 200: {System.Text.Json.JsonSerializer.Serialize(response)}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Internal server error: {ex.Message}" };
                Console.WriteLine($"Returning 500: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Customer,Admin,Kitchen")]
        public async Task<IActionResult> GetAllOrders()
        {
            Console.WriteLine($"Received GET /api/Order at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            Console.WriteLine($"User: {User.Identity?.Name ?? "Unknown"}, Roles: {string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value))}");
            Console.WriteLine($"All Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");

            try
            {
                var orders = await _orderService.GetAllAsync();
                Console.WriteLine($"Returning 200: {System.Text.Json.JsonSerializer.Serialize(orders)}");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Internal server error: {ex.Message}" };
                Console.WriteLine($"Returning 500: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Kitchen")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            Console.WriteLine($"Received PUT /api/Order/{id}/status at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
            Console.WriteLine($"Authorization Header: {Request.Headers["Authorization"]}");
            Console.WriteLine($"User: {User.Identity?.Name ?? "Unknown"}, Roles: {string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value))}");
            Console.WriteLine($"Request Body: {System.Text.Json.JsonSerializer.Serialize(request)}");

            if (!User.Identity.IsAuthenticated)
            {
                var errorResponse = new { error = "Unauthorized: Invalid or missing authentication token." };
                Console.WriteLine($"Returning 401: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return Unauthorized(errorResponse);
            }

            var validStatuses = new[] { "Received", "In Kitchen", "Ready", "Delivered" };
            if (string.IsNullOrEmpty(request?.Status) || !validStatuses.Contains(request.Status))
            {
                var errorResponse = new { error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}" };
                Console.WriteLine($"Returning 400: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return BadRequest(errorResponse);
            }

            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(id, request.Status);
                if (!success)
                {
                    var errorResponse = new { error = $"Order with ID {id} not found" };
                    Console.WriteLine($"Returning 404: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                    return NotFound(errorResponse);
                }

                var response = new { success = true, message = $"Order {id} status updated to {request.Status}" };
                Console.WriteLine($"Returning 200: {System.Text.Json.JsonSerializer.Serialize(response)}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new { error = $"Internal server error: {ex.Message}" };
                Console.WriteLine($"Returning 500: {System.Text.Json.JsonSerializer.Serialize(errorResponse)}");
                return StatusCode(500, errorResponse);
            }
        }
    }

    public class OrderRequest
    {
        [JsonPropertyName("order")]
        public required Order Order { get; set; }
    }

    public class UpdateStatusRequest
    {
        [JsonPropertyName("status")]
        public required string Status { get; set; }
    }
}