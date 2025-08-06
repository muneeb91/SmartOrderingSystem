using SmartOrderingSystem.Models;
using SmartOrderingSystem.Repositories;

namespace SmartOrderingSystem.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;
        public FeedbackService(IFeedbackRepository repository) => _repository = repository;
        public Task<List<Feedback>> GetAllAsync() => _repository.GetAllAsync();
        public Task<Feedback> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task AddAsync(Feedback feedback) => _repository.AddAsync(feedback);
    }
}
