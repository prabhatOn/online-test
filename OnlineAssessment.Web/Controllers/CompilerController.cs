using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Text.RegularExpressions;

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
                    return BadRequest(new { success = false, error = "Question not found" });
                }

                // Create a temporary Java file
                var className = "Solution";
                var filePath = Path.Combine(Path.GetTempPath(), $"{className}.java");
                var fullCode = GenerateFullJavaCode(className, request.Code, question);

                await System.IO.File.WriteAllTextAsync(filePath, fullCode);

                // First step: Compilation
                _logger.LogInformation("Compiling code...");
                var compileResult = await CompileJavaFile(filePath, className);
                if (!compileResult.Success)
                {
                    return Ok(new { 
                        success = false, 
                        phase = "compilation",
                        error = FormatCompilationError(compileResult.Error),
                        message = "Compilation Error"
                    });
                }

                _logger.LogInformation("Compilation successful. Running test cases...");

                // Second step: Run test cases
                var testResults = new List<TestCaseResult>();
                var allPassed = true;
                TestCase failedTestCase = null;
                string actualOutput = "";
                var totalTestCases = question.TestCases.Count;
                var currentTestCase = 0;

                foreach (var testCase in question.TestCases)
                {
                    currentTestCase++;
                    _logger.LogInformation($"Running test case {currentTestCase}/{totalTestCases}");

                    var result = await RunTestCase(className, testCase);
                    testResults.Add(result);

                    if (!result.Passed)
                    {
                        allPassed = false;
                        failedTestCase = testCase;
                        actualOutput = result.ActualOutput;
                        break;
                    }
                }

                // Clean up temporary files
                CleanupFiles(filePath, className);

                if (allPassed)
                {
                    return Ok(new
                    {
                        success = true,
                        phase = "execution",
                        message = "Accepted",
                        runtime = "0 ms",
                        testCaseResults = testResults.Select(r => new {
                            passed = r.Passed,
                            input = r.Input,
                            expectedOutput = r.ExpectedOutput,
                            actualOutput = r.ActualOutput
                        }).ToList(),
                        output = testResults.First().ActualOutput
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        phase = "execution",
                        message = "Wrong Answer",
                        error = "Test case failed",
                        failedTestCase = new
                        {
                            input = failedTestCase.Input,
                            expected = failedTestCase.ExpectedOutput,
                            actual = actualOutput,
                            testCaseNumber = currentTestCase
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running code");
                return Ok(new { 
                    success = false, 
                    phase = "execution",
                    error = ex.Message,
                    message = "Runtime Error"
                });
            }
        }

        private string FormatCompilationError(string error)
        {
            // Remove file paths and clean up the error message
            var lines = error.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => {
                    // Remove the file path from the error message
                    var match = Regex.Match(line, @"Solution\.java:(\d+):(.+)");
                    if (match.Success)
                    {
                        return $"Line {match.Groups[1].Value}: {match.Groups[2].Value.Trim()}";
                    }
                    return line.Trim();
                });
            return string.Join("\n", lines);
        }

        private string GenerateFullJavaCode(string className, string userCode, Question question)
        {
            var code = new StringBuilder();
            code.AppendLine("import java.util.*;");
            code.AppendLine($"public class {className} {{");
            
            // Add the user's solution method
            code.AppendLine(userCode);
            
            // Add the main method from the question
            if (!string.IsNullOrEmpty(question.MainMethod))
            {
                code.AppendLine(question.MainMethod);
            }
            else
            {
                // Fallback to default main method if not provided
                code.AppendLine("    public static void main(String[] args) {");
                code.AppendLine("        Scanner scanner = new Scanner(System.in);");
                code.AppendLine("        String input = scanner.nextLine().trim();");
                code.AppendLine($"        {className} solution = new {className}();");
                code.AppendLine($"        System.out.println(solution.{question.FunctionName}(input));");
                code.AppendLine("    }");
            }
            
            code.AppendLine("}");
            return code.ToString();
        }

        private async Task<(bool Success, string Error)> CompileJavaFile(string filePath, string className)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "javac",
                Arguments = filePath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode == 0, error);
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

            // Write input to the process
            await process.StandardInput.WriteLineAsync(testCase.Input);
            await process.StandardInput.FlushAsync();

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
                    Input = testCase.Input
                };
            }

            var normalizedOutput = NormalizeOutput(output.Trim());
            var normalizedExpected = NormalizeOutput(testCase.ExpectedOutput.Trim());

            return new TestCaseResult
            {
                Passed = normalizedOutput == normalizedExpected,
                ActualOutput = output.Trim(),
                ExpectedOutput = testCase.ExpectedOutput,
                Input = testCase.Input
            };
        }

        private string NormalizeOutput(string output)
        {
            // Remove all whitespace and convert to lowercase
            return Regex.Replace(output, @"\s+", "").ToLower();
        }

        private void CleanupFiles(string javaFilePath, string className)
        {
            try
            {
                if (System.IO.File.Exists(javaFilePath))
                    System.IO.File.Delete(javaFilePath);

                var classFilePath = Path.Combine(Path.GetTempPath(), $"{className}.class");
                if (System.IO.File.Exists(classFilePath))
                    System.IO.File.Delete(classFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up files");
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
    }
} 