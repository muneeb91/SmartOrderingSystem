using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartOrderingSystem.Models;
using SmartOrderingSystem.Services;
using System;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var menuItems = await _menuService.GetAllAsync();
                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to retrieve menu: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] MenuItem menuItem)
        {
            try
            {
                if (string.IsNullOrEmpty(menuItem.Name) || menuItem.Price <= 0)
                {
                    return BadRequest(new { error = "Invalid menu item: Name and positive price are required" });
                }
                var createdItem = await _menuService.CreateAsync(menuItem);
                return CreatedAtAction(nameof(GetAll), new { id = createdItem.Id }, createdItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to create menu item: {ex.Message}" });
            }
        }
    }
}