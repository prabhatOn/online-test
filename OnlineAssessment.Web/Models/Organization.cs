using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
