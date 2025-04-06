using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models
{
    public enum QuestionType
    {
        MultipleChoice,
        Coding
    }

    public class Question
    {
        public int Id { get; set; }

        [Required]
        public int TestId { get; set; }
        public virtual Test Test { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public QuestionType Type { get; set; }

        [Required]
        public int Points { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Answer> Answers { get; set; }
    }

    public class AnswerOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public int Order { get; set; }

        // Navigation properties
        public Question Question { get; set; }
    }
} 