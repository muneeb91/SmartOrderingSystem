using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public interface IFeedbackRepository
    {
        Task<List<Feedback>> GetAllAsync();
        Task<Feedback> GetByIdAsync(int id);
        Task AddAsync(Feedback feedback);
    }
}
