using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;
using SmartOrderingSystem.Services;
using System.Collections.Generic;

namespace SmartOrderingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService orderService = new OrderService();
        private readonly MenuRepository menuRepo = new MenuRepository();

        /*
        private (string Username, string Role)? GetUserFromToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Replace("Bearer ", "");
            return DemoJwt.Validate(token);
        }
        */

        public IActionResult Index()
        {
            // var user = GetUserFromToken();
            // if (user == null)
            //     return Unauthorized("Authentication required.");

            var orders = orderService.GetAll();
            return View(orders);
        }

        public IActionResult Details(int id)
        {
            // var user = GetUserFromToken();
            // if (user == null)
            //     return Unauthorized("Authentication required.");

            var order = orderService.GetById(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public IActionResult Create()
        {
            // var user = GetUserFromToken();
            // if (user == null)
            //     return Unauthorized("Authentication required.");

            ViewBag.MenuItems = menuRepo.GetAll();
            return View(new Order());
        }

        [HttpPost]
        public IActionResult Create(Order order)
        {
            // var user = GetUserFromToken();
            // if (user == null)
            //     return Unauthorized("Authentication required.");

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
            // var user = GetUserFromToken();
            // if (user == null || user.Value.Role != "Admin")
            //     return Unauthorized("Only Admin can update order status.");

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
            // var user = GetUserFromToken();
            // if (user == null || user.Value.Role != "Admin")
            //     return Unauthorized("Only Admin can update order status.");

            orderService.UpdateStatus(id, status);
            return RedirectToAction(nameof(Index));
        }
    }
}
