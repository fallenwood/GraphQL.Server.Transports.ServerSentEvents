using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Transports.ServerSentEvents.Samples.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok();
        }
    }
}
