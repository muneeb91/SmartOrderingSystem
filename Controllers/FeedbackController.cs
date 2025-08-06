using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Text;

namespace SmartOrderingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly DataContext _context;

        public FeedbackController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFeedback()
        {
            try
            {
                var feedback = await _context.Feedback.ToListAsync();
                var result = feedback.Select(f => new
                {
                    f.Id,
                    f.OrderId,
                    f.Rating,
                    f.Comment,
                    f.Sentiment,
                    Keywords = string.IsNullOrEmpty(f.Keywords) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(f.Keywords)
                });
                Console.WriteLine($"GET /api/Feedback Success: {feedback.Count} items");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET /api/Feedback Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                return StatusCode(500, new { error = $"Failed to fetch feedback: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
        {
            try
            {
                // Log raw request body
                Request.EnableBuffering();
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, false, leaveOpen: true))
                {
                    Request.Body.Position = 0;
                    var rawBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"POST /api/Feedback Raw Input: {rawBody}");
                }
                Request.Body.Position = 0;

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine($"POST /api/Feedback Error: Validation errors: {string.Join(", ", errors)}");
                    return BadRequest(new { errors });
                }
                Console.WriteLine($"POST /api/Feedback Parsed Input: {JsonSerializer.Serialize(feedback)}");
                var order = await _context.Orders.FindAsync(feedback.OrderId);
                if (order == null || (order.Status != "Delivered" && order.Status != "Received"))
                {
                    Console.WriteLine($"POST /api/Feedback Error: Invalid order {feedback.OrderId} or status {order?.Status}");
                    return BadRequest(new { error = "Feedback can only be submitted for delivered or received orders." });
                }
                if (string.IsNullOrEmpty(feedback.Comment))
                {
                    Console.WriteLine($"POST /api/Feedback Error: Comment field is required.");
                    return BadRequest(new { errors = new { Comment = new[] { "The Comment field is required." } } });
                }
                feedback.Comment = feedback.Comment.Trim();
                feedback.KeywordsList = feedback.KeywordsList ?? new List<string>();
                _context.Feedback.Add(feedback);
                await _context.SaveChangesAsync();
                Console.WriteLine($"POST /api/Feedback Success: Feedback saved for order {feedback.OrderId}, Comment: {feedback.Comment}");
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST /api/Feedback Error: Message={ex.Message}, StackTrace={ex.StackTrace}, InnerException={ex.InnerException?.Message}");
                return StatusCode(500, new { error = $"Failed to submit feedback: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }
    }
}