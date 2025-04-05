using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace OnlineAssessment.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TestController> _logger;

        public TestController(AppDbContext context, IWebHostEnvironment environment, ILogger<TestController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // View action for test list page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get user role from claims
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                ViewBag.IsAdmin = userRole == "Admin";
                ViewBag.Username = User.Identity?.Name ?? "Guest";

                // Get all tests from database
                var tests = await _context.Tests.ToListAsync();
                return View(tests);
            }
            catch (Exception)
            {
                // If there's an error, still show the page with empty tests
                ViewBag.IsAdmin = false;
                ViewBag.Username = "Guest";
                return View(new List<Test>());
            }
        }

        // View action for uploading questions
        [HttpGet]
        [Route("Test/upload-questions/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadQuestions(int id)
        {
            try
            {
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                return View("UploadQuestions", test);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading test: " + ex.Message });
            }
        }

        // View action for taking a test
        [HttpGet]
        [Route("Test/Take/{id}")]
        [Authorize]
        public async Task<IActionResult> Take(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        [HttpGet]
        [Route("Test/view-uploads")]
        [Authorize(Roles = "Admin")]
        public IActionResult ViewUploads()
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var files = Directory.GetFiles(uploadsFolder)
                .Select(f => new FileInfo(f))
                .Select(f => new
                {
                    Name = f.Name,
                    Size = f.Length,
                    LastModified = f.LastWriteTime,
                    Path = $"/uploads/{f.Name}"
                })
                .ToList();

            return View(files);
        }

        [HttpPost]
        [Route("Test/upload-questions")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestions([FromForm] IFormFile file, [FromForm] int testId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded or file is empty" });
                }

                if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Only JSON files are allowed" });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return Json(new { success = false, message = "File size exceeds 5MB limit" });
                }

                var test = await _context.Tests.FindAsync(testId);
                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found" });
                }

                // Read and process the file
                string jsonContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // Log the received JSON content
                _logger.LogInformation("Received JSON content: {JsonContent}", jsonContent);

                try
                {
                    var questions = JsonSerializer.Deserialize<List<QuestionDto>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (questions == null || !questions.Any())
                    {
                        return Json(new { success = false, message = "Invalid JSON format or empty questions" });
                    }

                    foreach (var questionDto in questions)
                    {
                        if (string.IsNullOrWhiteSpace(questionDto.Text))
                        {
                            return Json(new { success = false, message = "Question text cannot be empty" });
                        }

                        var question = new Question
                        {
                            Text = questionDto.Text,
                            Type = questionDto.Type,
                            TestId = testId,
                            MainMethod = questionDto.MainMethod ?? string.Empty,
                            FunctionName = questionDto.FunctionName,
                            ReturnType = questionDto.ReturnType,
                            ReturnDescription = questionDto.ReturnDescription,
                            Constraints = questionDto.Constraints,
                            StarterCode = questionDto.StarterCode,
                            Parameters = questionDto.Parameters
                        };

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();

                        if (questionDto.Type == QuestionType.MultipleChoice && questionDto.AnswerOptions != null)
                        {
                            foreach (var optionDto in questionDto.AnswerOptions)
                            {
                                if (string.IsNullOrWhiteSpace(optionDto.Text))
                                {
                                    return Json(new { success = false, message = "Answer option text cannot be empty" });
                                }

                                _context.AnswerOptions.Add(new AnswerOption
                                {
                                    Text = optionDto.Text,
                                    IsCorrect = optionDto.IsCorrect,
                                    QuestionId = question.Id
                                });
                            }
                        }
                        else if (questionDto.Type == QuestionType.ShortAnswer && questionDto.TestCases != null)
                        {
                            foreach (var testCaseDto in questionDto.TestCases)
                            {
                                if (string.IsNullOrWhiteSpace(testCaseDto.Input) || string.IsNullOrWhiteSpace(testCaseDto.ExpectedOutput))
                                {
                                    return Json(new { success = false, message = "Test case input and expected output cannot be empty" });
                                }

                                _context.TestCases.Add(new TestCase
                                {
                                    Input = testCaseDto.Input,
                                    ExpectedOutput = testCaseDto.ExpectedOutput,
                                    QuestionId = question.Id
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Questions uploaded and saved successfully" });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing JSON");
                    return Json(new { success = false, message = "Invalid JSON format: " + ex.Message });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving to database");
                    return Json(new { success = false, message = "Database error: " + ex.InnerException?.Message ?? ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                return Json(new { success = false, message = "Error processing file: " + ex.Message });
            }
        }

        [HttpDelete]
        [Route("Test/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTest(int id)
        {
            try
            {
                var test = await _context.Tests
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found." });
                }

                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Test deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting test: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("Test/Submit/{id}")]
        [Authorize]
        public async Task<IActionResult> Submit(int id, [FromBody] Dictionary<string, string> answers)
        {
            try
            {
                var test = await _context.Tests
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.TestCases)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                {
                    return NotFound();
                }

                int mcqCorrect = 0;
                int totalMcq = test.Questions.Count(q => q.Type == QuestionType.MultipleChoice);
                int codingCorrect = 0;
                int totalCoding = test.Questions.Count(q => q.Type == QuestionType.ShortAnswer);
                var evaluationDetails = new List<string>();

                // Evaluate MCQ questions
                foreach (var question in test.Questions.Where(q => q.Type == QuestionType.MultipleChoice))
                {
                    var questionNumber = test.Questions.ToList().IndexOf(question) + 1;
                    var selectedOptionId = answers.GetValueOrDefault($"question_{question.Id}");
                    
                    if (selectedOptionId != null)
                    {
                        var selectedOption = question.AnswerOptions.FirstOrDefault(a => a.Id.ToString() == selectedOptionId);
                        var isCorrect = selectedOption != null && selectedOption.IsCorrect;
                        if (isCorrect)
                        {
                            mcqCorrect++;
                        }
                        evaluationDetails.Add($"MCQ {questionNumber}: Selected: {selectedOption?.Text ?? "None"} - Correct: {isCorrect}");
                    }
                    else
                    {
                        evaluationDetails.Add($"MCQ {questionNumber}: No answer selected");
                    }
                }

                // Evaluate coding questions
                foreach (var question in test.Questions.Where(q => q.Type == QuestionType.ShortAnswer))
                {
                    var questionNumber = test.Questions.ToList().IndexOf(question) + 1;
                    var answer = answers.GetValueOrDefault($"question_{question.Id}");
                    
                    if (answer != null)
                    {
                        var testCase = question.TestCases.FirstOrDefault();
                        var isCorrect = testCase != null && answer.Trim().Equals(testCase.ExpectedOutput.Trim(), StringComparison.OrdinalIgnoreCase);
                        if (isCorrect)
                        {
                            codingCorrect++;
                        }
                        evaluationDetails.Add($"Coding {questionNumber}: Answer submitted - Passed test case: {isCorrect}");
                    }
                    else
                    {
                        evaluationDetails.Add($"Coding {questionNumber}: No answer provided");
                    }
                }

                // Calculate total score
                double mcqScore = totalMcq > 0 ? (double)mcqCorrect / totalMcq * 50 : 0; // MCQ worth 50%
                double codingScore = totalCoding > 0 ? (double)codingCorrect / totalCoding * 50 : 0; // Coding worth 50%
                double totalScore = mcqScore + codingScore;

                var result = new TestResult
                {
                    TestId = id,
                    Username = User.Identity?.Name ?? "Anonymous",
                    TotalQuestions = totalMcq + totalCoding,
                    CorrectAnswers = mcqCorrect + codingCorrect,
                    Score = totalScore,
                    McqScore = mcqScore,
                    CodingScore = codingScore,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.TestResults.Add(result);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    redirectUrl = $"/Test/Result/{result.Id}",
                    evaluationDetails = evaluationDetails,
                    score = totalScore,
                    mcqScore = mcqScore,
                    codingScore = codingScore,
                    mcqCorrect = mcqCorrect,
                    totalMcq = totalMcq,
                    codingCorrect = codingCorrect,
                    totalCoding = totalCoding
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error submitting test: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("Test/Result/{id}")]
        [Authorize]
        public async Task<IActionResult> Result(int id)
        {
            var result = await _context.TestResults
                .Include(r => r.Test)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }

        [HttpGet]
        [Route("Test/upload-coding-questions/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadCodingQuestions(int id)
        {
            try
            {
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                return View("UploadCodingQuestions", test);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading test: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("Test/upload-coding-questions")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCodingQuestions([FromForm] IFormFile file, [FromForm] int testId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded or file is empty" });
                }

                if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Only JSON files are allowed" });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return Json(new { success = false, message = "File size exceeds 5MB limit" });
                }

                var test = await _context.Tests.FindAsync(testId);
                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found" });
                }

                // Save file locally
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Read and process the file
                string jsonContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // Log the received JSON content
                _logger.LogInformation("Received JSON content: {JsonContent}", jsonContent);

                try
                {
                    // Deserialize the root object
                    var rootObject = JsonSerializer.Deserialize<JsonDocument>(jsonContent);
                    if (rootObject == null)
                    {
                        return Json(new { success = false, message = "Invalid JSON format: Could not parse JSON document" });
                    }

                    // Get the codingQuestions array
                    if (!rootObject.RootElement.TryGetProperty("codingQuestions", out var questionsElement))
                    {
                        return Json(new { success = false, message = "Invalid JSON format: Missing 'codingQuestions' property" });
                    }

                    // Deserialize the questions
                    var questions = JsonSerializer.Deserialize<List<QuestionDto>>(questionsElement.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (questions == null || !questions.Any())
                    {
                        return Json(new { success = false, message = "No questions found in the file" });
                    }

                    foreach (var questionDto in questions)
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(questionDto.Text))
                        {
                            return Json(new { success = false, message = "Question text cannot be empty" });
                        }

                        if (questionDto.Type != QuestionType.ShortAnswer)
                        {
                            return Json(new { success = false, message = "All questions must be of type ShortAnswer (1) for coding questions" });
                        }

                        if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                        {
                            return Json(new { success = false, message = "Coding questions must have at least one test case" });
                        }

                        if (string.IsNullOrWhiteSpace(questionDto.FunctionName))
                        {
                            return Json(new { success = false, message = "Function name is required for coding questions" });
                        }

                        if (string.IsNullOrWhiteSpace(questionDto.ReturnType))
                        {
                            return Json(new { success = false, message = "Return type is required for coding questions" });
                        }

                        if (questionDto.Parameters == null || !questionDto.Parameters.Any())
                        {
                            return Json(new { success = false, message = "At least one parameter is required for coding questions" });
                        }

                        if (questionDto.StarterCode == null || !questionDto.StarterCode.Any())
                        {
                            return Json(new { success = false, message = "Starter code is required for at least one programming language" });
                        }

                        // Create the question
                        var question = new Question
                        {
                            Text = questionDto.Text,
                            Type = questionDto.Type,
                            TestId = testId,
                            MainMethod = questionDto.MainMethod ?? string.Empty,
                            FunctionName = questionDto.FunctionName,
                            ReturnType = questionDto.ReturnType,
                            ReturnDescription = questionDto.ReturnDescription,
                            Constraints = questionDto.Constraints,
                            StarterCode = questionDto.StarterCode,
                            Parameters = questionDto.Parameters
                        };

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();

                        // Add test cases
                        foreach (var testCaseDto in questionDto.TestCases)
                        {
                            if (string.IsNullOrWhiteSpace(testCaseDto.Input) || string.IsNullOrWhiteSpace(testCaseDto.ExpectedOutput))
                            {
                                return Json(new { success = false, message = "Test case input and expected output cannot be empty" });
                            }

                            _context.TestCases.Add(new TestCase
                            {
                                Input = testCaseDto.Input,
                                ExpectedOutput = testCaseDto.ExpectedOutput,
                                Explanation = testCaseDto.Explanation,
                                QuestionId = question.Id
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Coding questions uploaded and saved successfully" });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization error");
                    return Json(new { success = false, message = $"Invalid JSON format: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file");
                    return Json(new { success = false, message = $"Error processing file: {ex.Message}" });
                }
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = "Invalid JSON format: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing file: " + ex.Message });
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TestApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestApiController(AppDbContext context)
        {
            _context = context;
        }

        // Create a new test
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTest([FromBody] Models.TestCreationDto testDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testDto.Title))
                    return BadRequest(new { message = "Test title is required" });

                if (testDto.DurationMinutes <= 0 || testDto.DurationMinutes > 1440)
                    return BadRequest(new { message = "Duration must be between 1 and 1440 minutes" });

                // Create the test
                var test = new Test
                {
                    Title = testDto.Title,
                    Description = testDto.Description ?? $"Test created on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    DurationMinutes = testDto.DurationMinutes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();

                // Add questions if provided
                if (testDto.Questions != null && testDto.Questions.Any())
                {
                    foreach (var questionDto in testDto.Questions)
                    {
                        if (string.IsNullOrWhiteSpace(questionDto.Text))
                            continue;

                        var question = new Question
                        {
                            Text = questionDto.Text,
                            Type = questionDto.Type,
                            TestId = test.Id
                        };

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();

                        if (questionDto.Type == QuestionType.MultipleChoice && questionDto.AnswerOptions != null)
                        {
                            foreach (var optionDto in questionDto.AnswerOptions)
                            {
                                _context.AnswerOptions.Add(new AnswerOption
                                {
                                    Text = optionDto.Text,
                                    IsCorrect = optionDto.IsCorrect,
                                    QuestionId = question.Id
                                });
                            }
                        }
                        else if (questionDto.Type == QuestionType.ShortAnswer && questionDto.TestCases != null)
                        {
                            foreach (var testCaseDto in questionDto.TestCases)
                            {
                                _context.TestCases.Add(new TestCase
                                {
                                    Input = testCaseDto.Input,
                                    ExpectedOutput = testCaseDto.ExpectedOutput,
                                    QuestionId = question.Id
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { 
                    message = "Test created successfully", 
                    test,
                    redirectUrl = $"/Test/Index"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error creating test: " + ex.Message });
            }
        }

        // Retrieve all tests
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTests()
        {
            var tests = await _context.Tests.ToListAsync();
            return Ok(tests);
        }
    }
}
