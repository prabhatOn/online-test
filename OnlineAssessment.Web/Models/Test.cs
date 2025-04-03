using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int DurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // ✅ Use UtcNow to avoid timezone issues

        // ✅ Navigation property for related questions
        public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
    }
}
