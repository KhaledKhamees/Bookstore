using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CatalogService.Data;
using CatalogService.Consumers;

namespace CatalogService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHostedService<PaymentProcessedConsumer>();
            builder.Services.AddDbContext<CatalogServiceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogServiceContext") ?? throw new InvalidOperationException("Connection string 'CatalogServiceContext' not found.")));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContextFactory<CatalogServiceContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogServiceContext")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
