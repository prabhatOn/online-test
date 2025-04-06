using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models;

public class Answer
{
    public int Id { get; set; }
    
    [Required]
    public int QuestionId { get; set; }
    
    public virtual Question Question { get; set; }
    
    [Required]
    [StringLength(1000)]
    public string Text { get; set; }
    
    [Required]
    public bool IsCorrect { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; }
    
    public virtual ICollection<TestResult> TestResults { get; set; }
} 