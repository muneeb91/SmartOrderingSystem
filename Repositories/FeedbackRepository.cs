using SmartOrderingSystem.Models;

namespace SmartOrderingSystem.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly List<Feedback> _feedbacks = new List<Feedback>();
        public Task<List<Feedback>> GetAllAsync() => Task.FromResult(_feedbacks);
        public Task<Feedback> GetByIdAsync(int id) => Task.FromResult(_feedbacks.FirstOrDefault(f => f.Id == id));
        public Task AddAsync(Feedback feedback)
        {
            feedback.Id = _feedbacks.Count > 0 ? _feedbacks.Max(f => f.Id) + 1 : 1;
            _feedbacks.Add(feedback);
            return Task.CompletedTask;
        }
    }
}
