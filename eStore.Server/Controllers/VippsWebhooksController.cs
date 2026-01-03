using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VippsWebhooksController : ControllerBase
    {
        private readonly ILogger<VippsWebhooksController> _logger;

        public VippsWebhooksController(ILogger<VippsWebhooksController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation("Vipps webhook received. Headers={Headers} Body={Body}", Request.Headers.ToString(), body);

            // TODO (later): validate webhook signature (Vipps has a webhook signature key from portal)
            // TODO (later): parse event, update PaymentTransaction + Order status

            return Ok();
        }
    }
}
