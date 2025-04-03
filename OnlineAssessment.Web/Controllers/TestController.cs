using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/tests")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        // View action for test list page
        [HttpGet]
        [Authorize]
        public IActionResult Index()
        {
            return View();
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

        // ✅ Create a new test
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTest([FromBody] Test test)
        {
            if (string.IsNullOrWhiteSpace(test.Title) || test.DurationMinutes <= 0)
                return BadRequest(new { message = "Invalid test details" });

            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Test created successfully", test });
        }

        // ✅ Retrieve all tests
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllTests()
        {
            var tests = await _context.Tests.ToListAsync();
            return Ok(tests);
        }
    }
}
