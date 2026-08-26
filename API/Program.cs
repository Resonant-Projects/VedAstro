using Microsoft.Extensions.Hosting;
using API.Security;

namespace API
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                    worker.UseMiddleware<ApiKeyMiddleware>())
                .Build();

            host.Run();
        }
    }
}
