using System.Text.Json.Serialization;
using OnlineAssessment.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public required string Text { get; set; }
        
        public QuestionType Type { get; set; }
        
        public int TestId { get; set; } // ✅ Foreign key (Required)

        [JsonIgnore] // ✅ Prevent circular reference in API response
        public Test? Test { get; set; } // ✅ Navigation property

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>(); // ✅ MCQ Options
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>(); // ✅ Coding test cases
    }
}
