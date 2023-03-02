using Microsoft.EntityFrameworkCore;
using WebApplication2.DAL;
using WebApplication2.Services;

namespace WebApplication2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // dependency injection configuration
            builder.Services.AddDbContext<ProductCatalogContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetValue<string>("CONNECTION_STRING"));
            });
            builder.Services.AddControllers();
            builder.Services.AddScoped<IProductService, ProductService>();

            var app = builder.Build();

            // middleware configuration
            app.MapControllers();

            app.Run();
        }
    }
}