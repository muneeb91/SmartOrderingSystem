using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public class MenuRepository : IMenuRepository
    {
        private readonly List<MenuItem> _items = new List<MenuItem>
    {
        new MenuItem { Id = 1, Name = "Zinger Burger", Price = 5.99m },
        new MenuItem { Id = 2, Name = "Fries", Price = 2.99m }
    };

        public Task<List<MenuItem>> GetAllAsync() => Task.FromResult(_items);
        public Task<MenuItem> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        public Task AddAsync(MenuItem item)
        {
            item.Id = _items.Max(i => i.Id) + 1;
            _items.Add(item);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(MenuItem item)
        {
            var existing = _items.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null)
            {
                existing.Name = item.Name;
                existing.Price = item.Price;
            }
            return Task.CompletedTask;
        }
        public Task DeleteAsync(int id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null) _items.Remove(item);
            return Task.CompletedTask;
        }
    }
}
