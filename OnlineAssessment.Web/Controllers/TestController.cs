using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/tests")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Create a new test
        [HttpPost("create")]
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
        public async Task<IActionResult> GetAllTests()
        {
            var tests = await _context.Tests.ToListAsync();
            return Ok(tests);
        }
    }
}
