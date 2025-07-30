using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;

namespace SmartOrderingSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly MenuRepository _repo;

        public MenuController()
        {
            _repo = new MenuRepository();
        }

        public IActionResult Index()
        {
            var items = _repo.GetAll();
            return View(items);
        }

        public IActionResult Create()
        {
            return View(new MenuItem());
        }

        [HttpPost]
        public IActionResult Create(MenuItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Price <= 0)
            {
                ModelState.AddModelError("", "Valid name and price are required.");
                return View(item);
            }

            _repo.Add(item);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var item = _repo.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(MenuItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Price <= 0)
            {
                ModelState.AddModelError("", "Valid name and price are required.");
                return View(item);
            }

            _repo.Update(item);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            _repo.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
