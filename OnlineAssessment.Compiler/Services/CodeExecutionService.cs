using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Text.Json;
using System.Linq;

namespace OnlineAssessment.Compiler.Services
{
    public class CodeExecutionService : ICodeExecutionService
    {
        private readonly Dictionary<string, ICompiler> _compilers;
        private readonly Dictionary<int, Question> _questions;

        public CodeExecutionService()
        {
            _compilers = new Dictionary<string, ICompiler>
            {
                { "java", new JavaCompiler() },
                { "python", new PythonCompiler() },
                { "csharp", new CSharpCompiler() }
            };

            // Load questions from JSON
            var questionsJson = File.ReadAllText("questions.json");
            var questionsData = JsonSerializer.Deserialize<QuestionsData>(questionsJson);
            _questions = questionsData.CodingQuestions.ToDictionary(q => q.TestId);
        }

        public async Task<CodeExecutionResult> ExecuteCodeAsync(string code, string language, int questionId)
        {
            var result = new CodeExecutionResult();
            var sw = new Stopwatch();
            
            try
            {
                if (!_compilers.TryGetValue(language.ToLower(), out var compiler))
                {
                    result.Success = false;
                    result.Error = $"Unsupported language: {language}";
                    return result;
                }

                if (!_questions.TryGetValue(questionId, out var question))
                {
                    result.Success = false;
                    result.Error = $"Question not found: {questionId}";
                    return result;
                }

                sw.Start();
                
                // Compile and execute the code
                var executionResult = await compiler.CompileAndExecuteAsync(code, question);
                
                sw.Stop();
                
                result.Success = executionResult.Success;
                result.Output = executionResult.Output;
                result.Error = executionResult.Error;
                result.Runtime = sw.Elapsed.TotalMilliseconds;
                result.Memory = executionResult.Memory;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Internal error: {ex.Message}";
            }

            return result;
        }
    }

    public interface ICompiler
    {
        Task<ExecutionResult> CompileAndExecuteAsync(string code, Question question);
    }

    public class JavaCompiler : ICompiler
    {
        public async Task<ExecutionResult> CompileAndExecuteAsync(string code, Question question)
        {
            // Implementation for Java compilation and execution
            // This would use a Docker container for sandboxing
            throw new NotImplementedException();
        }
    }

    public class PythonCompiler : ICompiler
    {
        public async Task<ExecutionResult> CompileAndExecuteAsync(string code, Question question)
        {
            // Implementation for Python execution
            // This would use a Docker container for sandboxing
            throw new NotImplementedException();
        }
    }

    public class CSharpCompiler : ICompiler
    {
        public async Task<ExecutionResult> CompileAndExecuteAsync(string code, Question question)
        {
            // Implementation for C# compilation and execution
            // This would use a Docker container for sandboxing
            throw new NotImplementedException();
        }
    }

    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public double Memory { get; set; }
    }

    public class QuestionsData
    {
        public List<Question> CodingQuestions { get; set; }
    }

    public class Question
    {
        public int TestId { get; set; }
        public string FunctionName { get; set; }
        public string ReturnType { get; set; }
        public List<TestCase> TestCases { get; set; }
    }

    public class TestCase
    {
        public string Input { get; set; }
        public string ExpectedOutput { get; set; }
        public string Explanation { get; set; }
    }
} 