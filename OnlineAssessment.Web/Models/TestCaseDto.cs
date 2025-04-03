using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models.DTOs
{
    public class TestCaseDto
    {
        [Required]
        public string Input { get; set; }

        [Required]
        public string ExpectedOutput { get; set; }
    }
}
