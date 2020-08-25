using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SimpleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly ILogger<HelloController> logger;

        public HelloController(ILogger<HelloController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello World";
        }
    }
}