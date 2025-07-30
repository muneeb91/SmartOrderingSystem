using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;
using SmartOrderingSystem.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService orderService = new OrderService();
        private readonly MenuRepository menuRepo = new MenuRepository();

        private const string FeedbackApiUrl = "http://192.168.18.97:5000/feedback";
        private const string AudioApiUrl = "http://192.168.18.97:5000/process_audio";

        public IActionResult Index()
        {
            var orders = orderService.GetAll();
            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var order = orderService.GetById(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public IActionResult Create()
        {
            ViewBag.MenuItems = menuRepo.GetAll();
            return View(new Order());
        }

        [HttpPost]
        public IActionResult Create(Order order)
        {
            if (order == null || order.Items == null || order.Items.Count == 0)
            {
                ModelState.AddModelError("", "Order must have at least one item.");
                ViewBag.MenuItems = menuRepo.GetAll();
                return View(order);
            }

            orderService.PlaceOrder(order);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult UpdateStatus(int id)
        {
            var order = orderService.GetById(id);
            if (order == null) return NotFound();

            ViewBag.StatusList = new List<OrderStatus>
            {
                OrderStatus.Pending,
                OrderStatus.InKitchen,
                OrderStatus.Ready,
                OrderStatus.Delivered
            };

            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, OrderStatus status)
        {
            orderService.UpdateStatus(id, status);
            return RedirectToAction(nameof(Index));
        }

        // -------------- GET Feedback form --------------
        public IActionResult Feedback(int orderId)
        {
            var order = orderService.GetById(orderId);
            if (order == null) return NotFound();

            var model = order.Items.Select(i => new Feedback
            {
                ItemName = i.MenuItemName
            }).ToList();

            return View(model);
        }

        // ------------- Submit Text Feedback -------------
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(List<Feedback> feedbackList)
        {
            using var httpClient = new HttpClient();

            var fullFeedbackText = string.Join(". ", feedbackList.Select(f => f.Comment).Where(c => !string.IsNullOrWhiteSpace(c)));

            var payload = new { text = fullFeedbackText };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(FeedbackApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                // Optionally log or show error
                Console.WriteLine($"❌ Feedback failed: {error}");
            }

            return RedirectToAction("Index");
        }

        // ------------- Submit Voice Feedback -------------
        [HttpPost]
        public async Task<IActionResult> SubmitVoiceFeedback(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest("No audio file provided.");

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();

            var stream = audioFile.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(audioFile.ContentType);

            content.Add(fileContent, "file", audioFile.FileName);

            var response = await httpClient.PostAsync(AudioApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Voice feedback failed: {error}");
            }

            return RedirectToAction("Index");
        }
    }
}
