using System.Threading.Tasks;

namespace OnlineAssessment.Compiler.Services
{
    public interface ICodeExecutionService
    {
        Task<CodeExecutionResult> ExecuteCodeAsync(string code, string language, int questionId);
    }

    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public double Runtime { get; set; }
        public double Memory { get; set; }
    }
} 