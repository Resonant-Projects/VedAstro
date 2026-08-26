using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace API.Security;

public sealed class ApiKeyMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        if (request is null)
        {
            await next(context);
            return;
        }

        var configuredKey = Environment.GetEnvironmentVariable(ApiKeyAuthentication.EnvironmentVariable);
        if (string.IsNullOrEmpty(configuredKey))
        {
            var unavailable = request.CreateResponse();
            unavailable.StatusCode = HttpStatusCode.ServiceUnavailable;
            context.GetInvocationResult().Value = unavailable;
            return;
        }

        var hasOneKey = request.Headers.TryGetValues(ApiKeyAuthentication.HeaderName, out var headerValues)
                        && headerValues is not null
                        && headerValues.Take(2).Count() == 1;
        var suppliedKey = hasOneKey ? headerValues!.Single() : null;

        if (!ApiKeyAuthentication.IsAuthorized(configuredKey, suppliedKey))
        {
            var unauthorized = request.CreateResponse();
            unauthorized.StatusCode = HttpStatusCode.Unauthorized;
            unauthorized.Headers.Add("WWW-Authenticate", "ApiKey");
            context.GetInvocationResult().Value = unauthorized;
            return;
        }

        await next(context);
    }
}
