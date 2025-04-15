// Central server API scaffold
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();

app.Run();

// Example API controller
namespace CentralServerApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        [HttpPost("checkin")]
        public IActionResult CheckIn([FromBody] AgentCheckInModel model) => Ok("Check-in received");

        [HttpPost("status")]
        public IActionResult Status([FromBody] AgentStatusModel model) => Ok("Status received");

        [HttpPost("command-response")]
        public IActionResult CommandResponse([FromBody] CommandResponseModel model) => Ok("Command response received");
    }

    public class AgentCheckInModel { public string AgentId { get; set; } public string Version { get; set; } }
    public class AgentStatusModel { public string AgentId { get; set; } public string Status { get; set; } }
    public class CommandResponseModel { public string AgentId { get; set; } public string CommandId { get; set; } public string Result { get; set; } }
}
