using Microsoft.AspNetCore.Mvc;

namespace RobotArm.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RobotController : ControllerBase
    {
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "online",
                message = "RobotArm API werkt"
            });
        }
    }
}