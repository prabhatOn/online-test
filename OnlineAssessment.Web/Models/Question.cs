using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public enum QuestionType
    {
        MCQ,
        Coding
    }

    public class Question
    {
        [Key]
        public int Id { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public int TestId { get; set; }

        [ForeignKey("TestId")]
        public Test Test { get; set; }

        public List<AnswerOption>? AnswerOptions { get; set; }  // Only for MCQs
        public List<TestCase>? TestCases { get; set; }  // Only for Coding questions
    }
}
