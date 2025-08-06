
using Microsoft.EntityFrameworkCore;
using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Services
{
    public class MenuService : IMenuService
    {
        private readonly DataContext _context;
        public MenuService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<MenuItem>> GetAllAsync()
        {
            return await _context.MenuItems.ToListAsync();
        }

        public async Task<MenuItem> GetByIdAsync(int id)
        {
            return await _context.MenuItems.FindAsync(id) ?? throw new KeyNotFoundException($"Menu item with ID {id} not found");
        }

        public async Task<MenuItem> CreateAsync(MenuItem item)
        {
            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task AddAsync(MenuItem item)
        {
            await CreateAsync(item);
        }

        public async Task UpdateAsync(MenuItem item)
        {
            var existingItem = await _context.MenuItems.FindAsync(item.Id);
            if (existingItem == null)
                throw new KeyNotFoundException($"Menu item with ID {item.Id} not found");
            existingItem.Name = item.Name;
            existingItem.Price = item.Price;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                throw new KeyNotFoundException($"Menu item with ID {id} not found");
            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}