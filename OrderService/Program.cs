using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Data;
using OrderService.Services.Catalog;
using OrderService.Services.RabbitMQ;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<OrderServiceContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("OrderServiceContext") ?? throw new InvalidOperationException("Connection string 'OrderServiceContext' not found.")));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            var bookCatalogUrl = builder.Configuration["ServiceUrls:Catalog"]
                ?? throw new InvalidOperationException("Book Catalog Url not found.");
            builder.Services.AddHttpClient<IBookCatalogClient, BookCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(bookCatalogUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

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
