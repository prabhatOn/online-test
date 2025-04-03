using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
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

        // ✅ Add an MCQ Question
        [HttpPost("add-mcq")]
        public async Task<IActionResult> AddMcqQuestion([FromBody] Question question)
        {
            if (question == null || question.TestId <= 0)
            {
                return BadRequest(new { message = "Invalid TestId" });
            }

            // ✅ Fetch the Test from the database
            var test = await _context.Tests.FindAsync(question.TestId);
            if (test == null)
            {
                return NotFound(new { message = "Test not found" });
            }

            question.Test = test;
            question.Type = QuestionType.MCQ; // ✅ Set type as MCQ

            if (question.AnswerOptions == null || !question.AnswerOptions.Any())
            {
                return BadRequest(new { message = "MCQ must have at least one answer option." });
            }

            foreach (var option in question.AnswerOptions)
            {
                option.Question = question; // ✅ Establish relationship
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "MCQ question added successfully!" });
        }

        // ✅ Add a Coding Question
        [HttpPost("add-coding")]
        public async Task<IActionResult> AddCodingQuestion([FromBody] Question question)
        {
            if (question == null || question.TestId <= 0 || question.TestCases == null || !question.TestCases.Any())
            {
                return BadRequest(new { message = "Invalid coding question format" });
            }

            var test = await _context.Tests.FindAsync(question.TestId);
            if (test == null)
            {
                return NotFound(new { message = "Test not found" });
            }

            question.Test = test;
            question.Type = QuestionType.Coding;

            foreach (var testCase in question.TestCases)
            {
                testCase.Question = question; // ✅ Ensure relationship
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Coding question added successfully!", question });
        }

        // ✅ Retrieve all questions for a test
        [HttpGet("test/{testId}")]
        public async Task<IActionResult> GetQuestionsByTestId(int testId)
        {
            var questions = await _context.Questions
                .Where(q => q.TestId == testId)
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .ToListAsync();

            if (questions == null || !questions.Any())
            {
                return NotFound(new { message = "No questions found for this test" });
            }

            return Ok(questions);
        }

        // ✅ Retrieve a single question by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Question not found" });
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
                return NotFound(new { message = "Question not found" });
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Question deleted successfully!" });
        }
    }
}
