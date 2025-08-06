using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartOrderingSystem.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Sentiment { get; set; }
        private List<string> _keywords = new List<string>();
        public string Keywords
        {
            get => JsonSerializer.Serialize(_keywords);
            set => _keywords = string.IsNullOrEmpty(value) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(value);
        }
        [NotMapped]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> KeywordsList
        {
            get => _keywords;
            set => _keywords = value ?? new List<string>();
        }
    }
}