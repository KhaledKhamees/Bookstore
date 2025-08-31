using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Data;
using OrderService.Services.Catalog;
using OrderService.Services.RabbitMQ;
using Prometheus;
using RabbitMQ.Client;
using Serilog;
using Serilog.Sinks.Elasticsearch;


namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region OldLogging
            // Configure Serilog
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console()
            //    .WriteTo.Seq("http://localhost:5341") 
            //    .CreateLogger();

            //Log.Logger = new LoggerConfiguration()
            //    .Enrich.FromLogContext()
            //    .Enrich.WithProperty("ServiceName", "OrderService") // Change per service
            //    .WriteTo.Console()
            //    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
            //    {
            //        AutoRegisterTemplate = true,
            //        IndexFormat = "orderservice-logs-{0:yyyy.MM.dd}"
            //    })
            //    .CreateLogger();
            #endregion
            // Create WebApplication builder
            var builder = WebApplication.CreateBuilder(args);
            // Configure Serilog from appsettings.json
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            // Configure DbContext with SQL Server
            builder.Services.AddDbContext<OrderServiceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("OrderServiceContext") ?? throw new InvalidOperationException("Connection string 'OrderServiceContext' not found.")));
            // Use Serilog for logging
            builder.Host.UseSerilog();
            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // Add Swagger generation
            builder.Services.AddSwaggerGen();
            // Configure HTTP client for BookCatalog service
            var bookCatalogUrl = builder.Configuration["ServiceUrls:Catalog"]
                ?? throw new InvalidOperationException("Book Catalog Url not found.");
            // Register services
            builder.Services.AddHttpClient<IBookCatalogClient, BookCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(bookCatalogUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            // Register RabbitMQ producer as a singleton
            builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            // Configure JWT authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["Jwt:Issuer"],
                            ValidAudience = builder.Configuration["Jwt:Audience"],
                            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                        };
                    });
            builder.Services.AddHealthChecks()
                    .AddSqlServer(
                        builder.Configuration.GetConnectionString("OrderServiceContext"),
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

            // Build the application
            var app = builder.Build();
            // Use Serilog request logging for HTTP requests and responses time measurement
            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseMetricServer();
            app.UseHttpMetrics();
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("app", _ => "OrderService");
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });


            app.MapControllers();

            app.Run();
        }
    }
}
