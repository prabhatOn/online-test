using System;
using System.Collections.Generic;

namespace OnlineAssessment.Infrastructure.Models
{
    public class CodingQuestion
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public int TestId { get; set; }
        public int Points { get; set; }
        public int Order { get; set; }
        public string FunctionName { get; set; }
        public string ReturnType { get; set; }
        public string StarterCode { get; set; }
        public string TestCases { get; set; }
        public string Constraints { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public Test Test { get; set; }
    }
} 