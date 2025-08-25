using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Consumers;
using PaymentService.Data;

namespace PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<PaymentServiceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentServiceContext") 
                        ?? throw new InvalidOperationException("Connection string 'PaymentServiceContext' not found.")));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHostedService<OrderPlacedConsumer>();
            builder.Services.AddDbContextFactory<PaymentServiceContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentServiceContext")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.MapGet("/", () => "Payment Service Running...");
            app.MapControllers();

            app.Run();
        }
    }
}
