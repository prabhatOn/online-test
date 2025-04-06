using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models
{
    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }
        public virtual Test Test { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        [Required]
        public int AnswerId { get; set; }
        public virtual Answer Answer { get; set; }

        public string CodeSubmission { get; set; }
        public string CompilationError { get; set; }
        public string RuntimeError { get; set; }
        public string Output { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 