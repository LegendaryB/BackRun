using BackRun.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BackRun.TestApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ILogger<JobsController> _logger;
        private readonly IBackRunJobEngine _engine;

        public JobsController(
            ILogger<JobsController> logger,
            IBackRunJobEngine engine)
        {
            _logger = logger;
            _engine = engine;
        }

        [HttpGet("Create")]
        public ActionResult CreateDummy()
        {
            var id = _engine.EnqueueAsync<SendWelcomeEmailPayload, SendWelcomeEmailHandler>(
                new SendWelcomeEmailPayload(),
                new ());

            return Ok(id);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BackRunJob>> Get(Guid id)
        {
            return await _engine.GetJobAsync(id);
        }
    }
}
