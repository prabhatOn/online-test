using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models.DTOs
{
    public class AnswerOptionDto
    {
        [Required]
        public string Text { get; set; }

        [Required]
        public bool IsCorrect { get; set; }
    }
}
