using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class TestCase
    {
        [Key]
        public int Id { get; set; }
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
    }
}
