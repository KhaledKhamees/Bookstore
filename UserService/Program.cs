using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using UserService.Data;
using UserService.Models;

namespace UserService
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

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHealthChecks()
                .AddSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    healthQuery: "SELECT 1;", // Simple query to check database connectivity
                    name: "sqlserver",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql", "sqlserver" }
                )
                .AddCheck("self", () => HealthCheckResult.Healthy());


            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<UserDbContext>()
                .AddDefaultTokenProviders();

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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(10); // time in seconds between check
                options.MaximumHistoryEntriesPerEndpoint(60); // maximum history of checks
                options.SetApiMaxActiveRequests(1); // api requests concurrency
                options.AddHealthCheckEndpoint("UserService", "/health"); // map health check endpoint
            }).AddInMemoryStorage(); 

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseMetricServer();
            app.UseHttpMetrics();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("app", _ => "UserService");
            });

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            // Health Checks endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecksUI(options => options.UIPath = "/health-ui");
            });

            app.MapControllers();

            app.Run();
        }
    }
}