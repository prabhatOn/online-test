using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using OnlineAssessment.Web.Models;
using OnlineAssessment.Compiler.Services;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeController : ControllerBase
    {
        private readonly ICodeExecutionService _codeExecutionService;

        public CodeController(ICodeExecutionService codeExecutionService)
        {
            _codeExecutionService = codeExecutionService;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunCode([FromBody] CodeExecutionRequest request)
        {
            try
            {
                var result = await _codeExecutionService.ExecuteCodeAsync(request.Code, request.Language, request.QuestionId);
                return Ok(new
                {
                    success = result.Success,
                    runtime = result.Runtime,
                    memory = result.Memory,
                    error = result.Error,
                    output = result.Output
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
} 