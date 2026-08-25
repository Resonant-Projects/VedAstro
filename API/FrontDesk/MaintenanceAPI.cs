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
        var sourceRevision = Environment.GetEnvironmentVariable("VEDASTRO_SOURCE_REVISION");
        if (string.IsNullOrWhiteSpace(sourceRevision))
        {
            sourceRevision = ThisAssembly.CommitHash;
        }

        return APITools.PassMessageJson(new { Version = sourceRevision }, incomingRequest);
    }
}
