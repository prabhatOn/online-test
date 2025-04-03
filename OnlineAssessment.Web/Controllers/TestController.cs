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

namespace OnlineAssessment.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
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
        [HttpGet("take/{id}")]
        [Authorize]
        public async Task<IActionResult> Take(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        [HttpPost]
        [Route("upload-questions")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestions([FromForm] IFormFile file, [FromForm] int testId)
        {
            try
            {
                if (file == null)
                {
                    return Json(new { success = false, message = "No file was uploaded" });
                }

                if (file.Length == 0)
                {
                    return Json(new { success = false, message = "The uploaded file is empty" });
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

                string jsonContent;
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return Json(new { success = false, message = "File is empty" });
                }

                var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionDto>>(jsonContent);

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
                        TestId = testId
                    };

                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

                    if (questionDto.Type == QuestionType.MultipleChoice)
                    {
                        if (questionDto.AnswerOptions == null || !questionDto.AnswerOptions.Any())
                        {
                            return Json(new { success = false, message = "Multiple choice questions must have answer options" });
                        }

                        foreach (var optionDto in questionDto.AnswerOptions)
                        {
                            if (string.IsNullOrWhiteSpace(optionDto.Text))
                            {
                                return Json(new { success = false, message = "Answer option text cannot be empty" });
                            }

                            var option = new AnswerOption
                            {
                                Text = optionDto.Text,
                                IsCorrect = optionDto.IsCorrect,
                                QuestionId = question.Id
                            };
                            _context.AnswerOptions.Add(option);
                        }
                    }
                    else if (questionDto.Type == QuestionType.ShortAnswer)
                    {
                        if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                        {
                            return Json(new { success = false, message = "Short answer questions must have test cases" });
                        }

                        foreach (var testCaseDto in questionDto.TestCases)
                        {
                            if (string.IsNullOrWhiteSpace(testCaseDto.Input) || string.IsNullOrWhiteSpace(testCaseDto.ExpectedOutput))
                            {
                                return Json(new { success = false, message = "Test case input and expected output cannot be empty" });
                            }

                            var testCase = new TestCase
                            {
                                Input = testCaseDto.Input,
                                ExpectedOutput = testCaseDto.ExpectedOutput,
                                QuestionId = question.Id
                            };
                            _context.TestCases.Add(testCase);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Questions uploaded successfully" });
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
        public async Task<IActionResult> CreateTest([FromBody] Test test)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(test.Title))
                    return BadRequest(new { message = "Test title is required" });

                if (test.DurationMinutes <= 0 || test.DurationMinutes > 1440)
                    return BadRequest(new { message = "Duration must be between 1 and 1440 minutes" });

                // Set a default description if none is provided
                if (string.IsNullOrWhiteSpace(test.Description))
                {
                    test.Description = $"Test created on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                }

                // Set CreatedAt to current time
                test.CreatedAt = DateTime.UtcNow;

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Test created successfully", 
                    test,
                    redirectUrl = $"/Test/upload-questions/{test.Id}"
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
