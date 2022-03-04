using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ToyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls(new[] { "http://localhost:44374" });
                });
    }
}
