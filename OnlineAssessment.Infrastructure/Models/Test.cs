using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models
{
    public class Test
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public int DurationInMinutes { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        [Required]
        public int CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; }
        
        [Required]
        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; }
        
        // Navigation properties
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<TestResult> TestResults { get; set; }
    }
} 