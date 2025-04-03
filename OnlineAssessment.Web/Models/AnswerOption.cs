using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OptionText { get; set; }

        public bool IsCorrect { get; set; }

        // ✅ Foreign Key
        public int QuestionId { get; set; }

        // ✅ Navigation Property (Make it Nullable to Prevent Validation Errors)
        public Question? Question { get; set; }
    }
}
