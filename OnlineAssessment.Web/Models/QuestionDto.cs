using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class QuestionDto
    {
        public required string Text { get; set; }
        public required QuestionType Type { get; set; }
        public required int TestId { get; set; }
        public List<AnswerOptionDto>? AnswerOptions { get; set; }
        public List<TestCaseDto>? TestCases { get; set; }
    }
}
