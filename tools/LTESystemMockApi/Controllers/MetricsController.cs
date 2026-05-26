using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LTESystemMockApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController(ILogger<MetricsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<MetricAcceptedResponse>(StatusCodes.Status200OK)]
    public ActionResult<MetricAcceptedResponse> Receive([FromBody] JsonElement payload)
    {
        var receivedAtUtc = DateTimeOffset.UtcNow;

        logger.LogInformation("Metric payload received at {ReceivedAtUtc}: {Payload}",
            receivedAtUtc,
            payload.GetRawText());

        return Ok(new MetricAcceptedResponse(true, receivedAtUtc));
    }
}

public sealed record MetricAcceptedResponse(bool Accepted, DateTimeOffset ReceivedAtUtc);
