using SmartOrderingSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartOrderingSystem.Services
{
    public interface IMenuService
    {
        Task<List<MenuItem>> GetAllAsync();
        Task<MenuItem> GetByIdAsync(int id);
        Task<MenuItem> CreateAsync(MenuItem item);
        Task AddAsync(MenuItem item);
        Task UpdateAsync(MenuItem item);
        Task DeleteAsync(int id);
    }
}