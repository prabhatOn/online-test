using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public string Answer { get; set; }
    }
}
