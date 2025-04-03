
using System.Text.Json.Serialization;
using OnlineAssessment.Web.Models; 


public class Question
{
    public int Id { get; set; }
    public string Text { get; set; }

    public int TestId { get; set; } // ✅ Foreign key (Required)

    [JsonIgnore] // ✅ Prevent circular reference in API response
    public Test? Test { get; set; } // ✅ Navigation property

    public QuestionType Type { get; set; } // ✅ Now correctly references the enum

    public List<AnswerOption>? AnswerOptions { get; set; } = new(); // ✅ MCQ Options

    public List<TestCase>? TestCases { get; set; } = new(); // ✅ Coding test cases
}
