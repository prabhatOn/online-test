using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
            if (string.IsNullOrWhiteSpace(test.Title) || test.DurationMinutes <= 0)
                return BadRequest(new { message = "Invalid test details" });

            // Set a default description if none is provided
            if (string.IsNullOrWhiteSpace(test.Description))
            {
                test.Description = $"Test created on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            }

            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Test created successfully", test });
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
