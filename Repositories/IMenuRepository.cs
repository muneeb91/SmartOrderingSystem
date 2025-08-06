using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public interface IMenuRepository
    {
        Task<List<MenuItem>> GetAllAsync();
        Task<MenuItem> GetByIdAsync(int id);
        Task AddAsync(MenuItem item);
        Task UpdateAsync(MenuItem item);
        Task DeleteAsync(int id);
    }
}
