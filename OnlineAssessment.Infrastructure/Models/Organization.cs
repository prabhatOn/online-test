using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models
{
    public class Organization
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ContactEmail { get; set; }
        
        [StringLength(20)]
        public string ContactPhone { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; }
        
        public int CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; }
        
        // Navigation properties
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Test> Tests { get; set; }
    }
} 