using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using OnlineAssessment.Web.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionController : Controller
    {
        private readonly AppDbContext _context;

        public QuestionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto questionDto)
        {
            if (string.IsNullOrWhiteSpace(questionDto.Text))
                return BadRequest(new { message = "Question text is required" });

            var question = new Question
            {
                Text = questionDto.Text,
                Type = questionDto.Type,
                TestId = questionDto.TestId
            };

            if (questionDto.Type == QuestionType.MultipleChoice)
            {
                if (questionDto.AnswerOptions == null || questionDto.AnswerOptions.Count < 2)
                    return BadRequest(new { message = "Multiple choice questions require at least 2 answer options" });

                question.AnswerOptions = questionDto.AnswerOptions.Select(option => new AnswerOption
                {
                    Text = option.Text,
                    IsCorrect = option.IsCorrect
                }).ToList();
            }
            else if (questionDto.Type == QuestionType.ShortAnswer)
            {
                if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                    return BadRequest(new { message = "Short answer questions require at least one test case" });

                question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput
                }).ToList();
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question created successfully", question });
        }

        [HttpGet("test/{testId}")]
        [Authorize]
        public async Task<IActionResult> GetQuestionsByTest(int testId)
        {
            var questions = await _context.Questions
                .Where(q => q.TestId == testId)
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .ToListAsync();

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
