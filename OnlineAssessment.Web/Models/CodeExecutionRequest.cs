using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class CodeExecutionRequest
    {
        public string Code { get; set; }
        public string Language { get; set; }
        public int QuestionId { get; set; }
    }
} 