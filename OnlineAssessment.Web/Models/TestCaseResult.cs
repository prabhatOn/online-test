using System.Collections.Generic;

namespace OnlineAssessment.Web.Models
{
    public class TestCaseResult
    {
        public bool Passed { get; set; }
        public string Input { get; set; }
        public string ActualOutput { get; set; }
        public string ExpectedOutput { get; set; }
        public string Explanation { get; set; }
        public int CaseNumber { get; set; }
        public bool ShowDetails { get; set; }
    }

    public class CodeExecutionResponse
    {
        public bool Success { get; set; }
        public string CompilationOutput { get; set; }
        public List<TestCaseResult> TestCaseResults { get; set; }
        public string ErrorMessage { get; set; }
        public long RuntimeInMs { get; set; }
        public string Status { get; set; }
    }
} 