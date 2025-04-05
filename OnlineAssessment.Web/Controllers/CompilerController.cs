using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompilerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompilerController> _logger;

        public CompilerController(AppDbContext context, ILogger<CompilerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunCode([FromBody] CodeExecutionRequest request)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.TestCases)
                    .FirstOrDefaultAsync(q => q.Id == request.QuestionId);

                if (question == null)
                {
                    return BadRequest(new { success = false, message = "Question not found" });
                }

                var className = $"Solution_{Guid.NewGuid().ToString("N")}";
                var fullCode = GenerateFullJavaCode(className, request.Code, question);
                var javaFilePath = Path.Combine(Path.GetTempPath(), $"{className}.java");

                try
                {
                    // Write the code to a temporary file
                    await System.IO.File.WriteAllTextAsync(javaFilePath, fullCode);

                    // Compile the code
                    var startTime = DateTime.UtcNow;
                    var compileResult = await CompileJavaCode(javaFilePath);
                    
                    if (!compileResult.Success)
                    {
                        return Ok(new CodeExecutionResponse
                        {
                            Success = false,
                            ErrorMessage = "Compilation Error: " + compileResult.Error,
                            Status = "Compilation Error"
                        });
                    }

                    // Run test cases
                    var testResults = new List<TestCaseResult>();
                    foreach (var testCase in question.TestCases)
                    {
                        var result = await RunTestCase(className, testCase);
                        testResults.Add(result);
                    }

                    var endTime = DateTime.UtcNow;
                    var runtimeMs = (long)(endTime - startTime).TotalMilliseconds;

                    return Ok(new CodeExecutionResponse
                    {
                        Success = true,
                        TestCaseResults = testResults,
                        RuntimeInMs = runtimeMs,
                        Status = testResults.All(r => r.Passed) ? "Accepted" : "Wrong Answer"
                    });
                }
                finally
                {
                    // Cleanup temporary files
                    try
                    {
                        if (System.IO.File.Exists(javaFilePath))
                            System.IO.File.Delete(javaFilePath);
                        var classFilePath = Path.Combine(Path.GetTempPath(), $"{className}.class");
                        if (System.IO.File.Exists(classFilePath))
                            System.IO.File.Delete(classFilePath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running code");
                return Ok(new CodeExecutionResponse
                {
                    Success = false,
                    ErrorMessage = "Internal error: " + ex.Message,
                    Status = "Internal Error"
                });
            }
        }

        private async Task<(bool Success, string Error)> CompileJavaCode(string javaFilePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "javac",
                Arguments = javaFilePath,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode == 0, error);
        }

        private string GenerateFullJavaCode(string className, string userCode, Question question)
        {
            var code = new StringBuilder();
            code.AppendLine("import java.util.*;");
            code.AppendLine($"public class {className} {{");
            code.AppendLine(userCode);
            
            if (!string.IsNullOrEmpty(question.MainMethod))
            {
                code.AppendLine(question.MainMethod);
            }
            else
            {
                // Fallback main method if none provided
                code.AppendLine("    public static void main(String[] args) {");
                code.AppendLine("        Scanner scanner = new Scanner(System.in);");
                code.AppendLine("        String input = scanner.nextLine();");
                code.AppendLine($"        System.out.println(new {className}().{question.FunctionName}(input));");
                code.AppendLine("    }");
            }
            
            code.AppendLine("}");
            return code.ToString();
        }

        private async Task<TestCaseResult> RunTestCase(string className, TestCase testCase)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-cp {Path.GetTempPath()} {className}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            try
            {
                // Write input to the process
                if (!string.IsNullOrEmpty(testCase.Input))
                {
                    await process.StandardInput.WriteLineAsync(testCase.Input);
                    await process.StandardInput.FlushAsync();
                }
                process.StandardInput.Close();

                // Read output and error
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    return new TestCaseResult
                    {
                        Passed = false,
                        ActualOutput = "Runtime Error: " + error,
                        ExpectedOutput = testCase.ExpectedOutput,
                        Input = testCase.Input,
                        Explanation = testCase.Explanation
                    };
                }

                var normalizedOutput = output.Trim();
                var normalizedExpected = testCase.ExpectedOutput.Trim();

                return new TestCaseResult
                {
                    Passed = normalizedOutput.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase),
                    ActualOutput = output.Trim(),
                    ExpectedOutput = testCase.ExpectedOutput,
                    Input = testCase.Input,
                    Explanation = testCase.Explanation
                };
            }
            catch (Exception ex)
            {
                return new TestCaseResult
                {
                    Passed = false,
                    ActualOutput = "Error: " + ex.Message,
                    ExpectedOutput = testCase.ExpectedOutput,
                    Input = testCase.Input,
                    Explanation = testCase.Explanation
                };
            }
        }
    }

    public class CodeExecutionRequest
    {
        public string Code { get; set; }
        public string Language { get; set; }
        public int QuestionId { get; set; }
    }

    public class TestCaseResult
    {
        public bool Passed { get; set; }
        public string ActualOutput { get; set; }
        public string ExpectedOutput { get; set; }
        public string Input { get; set; }
        public string Explanation { get; set; }
        public int CaseNumber { get; set; }
        public bool ShowDetails { get; set; }
    }

    public class CodeExecutionResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Status { get; set; }
        public List<TestCaseResult> TestCaseResults { get; set; }
        public long RuntimeInMs { get; set; }
    }
} 