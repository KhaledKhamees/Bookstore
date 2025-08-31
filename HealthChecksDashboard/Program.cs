using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HealthChecksDashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Services.AddHealthChecksUI(options =>
            {
                options.AddHealthCheckEndpoint("User Service", "http://localhost:5215/health");
                options.AddHealthCheckEndpoint("Order Service", "http://localhost:5001/health");
                options.AddHealthCheckEndpoint("Catalog Service", "http://localhost:5231/health");
                options.AddHealthCheckEndpoint("Payment Service", "http://localhost:5091/health");
            }).AddInMemoryStorage();
            builder.Host.UseSerilog();
            var app = builder.Build();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecksUI(options => options.UIPath = "/dashboard");
            });

            app.Run();
        }
    }
}
