using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Consumers;
using PaymentService.Data;
using Prometheus;
using Serilog;

namespace PaymentService
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
            builder.Host.UseSerilog();


            builder.Services.AddDbContext<PaymentServiceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentServiceContext") 
                        ?? throw new InvalidOperationException("Connection string 'PaymentServiceContext' not found.")));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHostedService<OrderPlacedConsumer>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            builder.Services.AddHealthChecks()
                    .AddSqlServer(
                        builder.Configuration.GetConnectionString("PaymentServiceContext"),
                        healthQuery: "SELECT 1;",
                        name: "sqlserver",
                        tags: new[] { "db", "sql", "sqlserver" }
                    ).AddRabbitMQ(
                        name: "rabbitmq",
                        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                        tags: new[] { "mq", "rabbitmq" },
                        factory: sp =>
                        {
                            var factory = new RabbitMQ.Client.ConnectionFactory()
                            {
                                HostName = builder.Configuration["RabbitMQ:Host"],
                                Port = int.Parse(builder.Configuration["RabbitMQ:Port"]),
                                UserName = builder.Configuration["RabbitMQ:Username"],
                                Password = builder.Configuration["RabbitMQ:Password"],
                            };
                            return factory.CreateConnectionAsync();
                        }
                    )
                    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());


            var app = builder.Build();
            // Use Serilog request logging for HTTP requests and responses time measurement
            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseMetricServer();
            app.UseHttpMetrics();
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("app", _ => "PaymentService");
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });

            app.UseAuthorization();
            app.MapGet("/", () => "Payment Service Running...");
            app.MapControllers();

            app.Run();
        }
    }
}
