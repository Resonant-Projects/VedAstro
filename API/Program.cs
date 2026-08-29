using Microsoft.Extensions.Hosting;
using API.Security;
using VedAstro.Library;

namespace API
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            EphemerisFactory.ValidateEphemerisFiles();
            Console.WriteLine($"Swiss Ephemeris files are active at: {EphemerisFactory.EphemerisFilesPath}");

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                    worker.UseMiddleware<ApiKeyMiddleware>())
                .Build();

            host.Run();
        }
    }
}
