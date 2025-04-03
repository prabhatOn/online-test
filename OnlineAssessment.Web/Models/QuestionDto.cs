using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models.DTOs
{
    public class QuestionDto
    {
        [Required]
        public string Text { get; set; }

        [Required]
        public int TestId { get; set; }

        public List<TestCaseDto> TestCases { get; set; } = new List<TestCaseDto>();
    }
}
