using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Services
{
    public interface IFeedbackService
    {
        Task<List<Feedback>> GetAllAsync();
        Task<Feedback> GetByIdAsync(int id);
        Task AddAsync(Feedback feedback);
    }
}
