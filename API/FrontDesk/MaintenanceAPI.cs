using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using VedAstro.Library;

namespace API;

/// <summary>Operational endpoints used to identify and health-check a deployment.</summary>
public static class MaintenanceAPI
{
    [Function(nameof(Version))]
    public static HttpResponseData Version(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")]
        HttpRequestData incomingRequest)
    {
        _ = OpenAPI.StartCalculatorWarmup();
        if (!OpenAPI.IsCalculatorReady)
        {
            var warmingUp = incomingRequest.CreateResponse(HttpStatusCode.ServiceUnavailable);
            warmingUp.Headers.Add("Retry-After", "1");
            warmingUp.WriteString("Calculator engine is warming up.");
            return warmingUp;
        }

        var sourceRevision = Environment.GetEnvironmentVariable("VEDASTRO_SOURCE_REVISION");
        if (string.IsNullOrWhiteSpace(sourceRevision))
        {
            sourceRevision = ThisAssembly.CommitHash;
        }

        return APITools.PassMessageJson(new { Version = sourceRevision }, incomingRequest);
    }
}
