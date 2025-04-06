using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Infrastructure.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        
        public virtual ICollection<Organization> Organizations { get; set; }
        public virtual ICollection<Test> Tests { get; set; }
    }
} 