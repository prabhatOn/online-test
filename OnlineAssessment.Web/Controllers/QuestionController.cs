using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using OnlineAssessment.Web.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuestionController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Add a Coding Question (Fixed)
        [HttpPost("add-coding")]
        public async Task<IActionResult> AddCodingQuestion([FromBody] QuestionDto questionDto)
        {
            if (questionDto == null || string.IsNullOrWhiteSpace(questionDto.Text) || questionDto.TestId <= 0)
            {
                return BadRequest(new { message = "Invalid question data." });
            }

            // ✅ Check if the test exists
            var test = await _context.Tests.FindAsync(questionDto.TestId);
            if (test == null)
            {
                return NotFound(new { message = "Test not found" });
            }

            // ✅ Create the Question entity
            var question = new Question
            {
                Text = questionDto.Text,
                TestId = questionDto.TestId,
                Test = test,
                Type = QuestionType.Coding,
                TestCases = questionDto.TestCases?.Select(tc => new TestCase
                {
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput
                }).ToList() ?? new List<TestCase>()
            };

            if (!question.TestCases.Any())
            {
                return BadRequest(new { message = "Coding question must have at least one test case." });
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Coding question added successfully!", questionId = question.Id });
        }

        // ✅ Add an MCQ Question (Fixed)
        [HttpPost("add-mcq")]
        public async Task<IActionResult> AddMcqQuestion([FromBody] McqQuestionDto questionDto)
        {
            if (questionDto == null || string.IsNullOrWhiteSpace(questionDto.Text) || questionDto.TestId <= 0)
            {
                return BadRequest(new { message = "Invalid question data." });
            }

            var test = await _context.Tests.FindAsync(questionDto.TestId);
            if (test == null)
            {
                return NotFound(new { message = "Test not found" });
            }

            var question = new Question
            {
                Text = questionDto.Text,
                TestId = questionDto.TestId,
                Test = test,
                Type = QuestionType.MCQ,
                AnswerOptions = questionDto.AnswerOptions?.Select(option => new AnswerOption
                {
                    Text = option.Text,
                    IsCorrect = option.IsCorrect
                }).ToList() ?? new List<AnswerOption>()
            };

            if (!question.AnswerOptions.Any())
            {
                return BadRequest(new { message = "MCQ must have at least one answer option." });
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "MCQ question added successfully!", questionId = question.Id });
        }

        // ✅ Get all questions for a test
        [HttpGet("test/{testId}")]
        public async Task<IActionResult> GetQuestionsByTestId(int testId)
        {
            var questions = await _context.Questions
                .Where(q => q.TestId == testId)
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .ToListAsync();

            if (!questions.Any())
            {
                return NotFound(new { message = "No questions found for this test." });
            }

            return Ok(questions);
        }

        // ✅ Get a single question by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Question not found." });
            }

            return Ok(question);
        }

        // ✅ Delete a question
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Question not found." });
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Question deleted successfully!" });
        }
    }
}
