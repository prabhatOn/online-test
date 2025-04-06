using Microsoft.AspNetCore.Mvc;
using OnlineAssessment.Compiler.Services;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;

namespace OnlineAssessment.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodeExecutionController : ControllerBase
    {
        private readonly ICodeExecutionService _codeExecutionService;
        private readonly ILogger<CodeExecutionController> _logger;

        public CodeExecutionController(
            ICodeExecutionService codeExecutionService,
            ILogger<CodeExecutionController> logger)
        {
            _codeExecutionService = codeExecutionService;
            _logger = logger;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteCode([FromBody] CodeExecutionRequest request)
        {
            try
            {
                _logger.LogInformation($"Executing code for question {request.QuestionId} in {request.Language}");
                
                var result = await _codeExecutionService.ExecuteCodeAsync(
                    request.Code,
                    request.Language,
                    request.QuestionId);

                if (!result.Success)
                {
                    _logger.LogWarning($"Code execution failed: {result.Error}");
                    return BadRequest(new { error = result.Error });
                }

                return Ok(new
                {
                    success = true,
                    output = result.Output,
                    runtime = result.Runtime,
                    memory = result.Memory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing code");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
} 