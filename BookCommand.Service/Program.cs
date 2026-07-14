using System.Text;
using BookCommand.Service.Context;
using BookCommand.Service.Middleware;
using BookCommand.Service.Models;
using BookCommand.Service.MQ;
using BookCommand.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace BookCommand.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            builder.Services.AddDbContext<BookContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            var authEnabled = configuration.GetValue("Authentication:Enabled", false);
            if (authEnabled)
            {
                builder.Services.AddControllers(options =>
                {
                    options.Filters.Add(new AuthorizeFilter());
                });
            }
            else
            {
                builder.Services.AddControllers();
            }

            builder.Services.AddRabbitMQ(configuration);
            builder.Services.Configure<RabbitMQConfig>(configuration.GetSection("RabbitMQConfig"));
            builder.Services.AddScoped<IBookService, BookService>();
            builder.Services.AddHostedService<OutboxPublisherService>();

            ConfigureAuthentication(builder.Services, configuration, authEnabled);

            builder.Services.AddHealthChecks()
                .AddSqlServer(connectionString, name: "sqlserver")
                .AddRabbitMQ(BuildRabbitMqConnectionString(configuration), name: "rabbitmq");

            builder.Services.AddSwaggerGen(config =>
            {
                config.SwaggerDoc("v1.0.0", new OpenApiInfo
                {
                    Title = "Book Command Service",
                    Version = "v1.0.0"
                });

                if (authEnabled)
                {
                    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme.",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer"
                    });
                    config.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                }
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BookContext>();
                if (db.Database.IsRelational())
                {
                    EnsureDatabaseExists(connectionString);
                    db.Database.Migrate();
                }
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(config =>
                {
                    config.SwaggerEndpoint("/swagger/v1.0.0/swagger.json", "Books Command System");
                });
            }

            if (authEnabled)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            app.MapControllers();
            app.MapHealthChecks("/health");
            app.Run();
        }

        private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration, bool authEnabled)
        {
            if (!authEnabled)
            {
                return;
            }

            var jwtSection = configuration.GetSection("Authentication:Jwt");
            var key = jwtSection["Key"] ?? "BookCatalogDevSigningKey_ChangeMe_32chars!";
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"] ?? "BookCatalog",
                        ValidAudience = jwtSection["Audience"] ?? "BookCatalog",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                    };
                });
            services.AddAuthorization();
        }

        private static void EnsureDatabaseExists(string connectionString)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return;
            }

            builder.InitialCatalog = "master";
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            var safeName = databaseName.Replace("]", "]]");
            command.CommandText = $"IF DB_ID(N'{safeName}') IS NULL CREATE DATABASE [{safeName}]";
            command.ExecuteNonQuery();
        }

        private static string BuildRabbitMqConnectionString(IConfiguration configuration)
        {
            var cfg = configuration.GetSection("RabbitMQConfig");
            var user = cfg["UserName"] ?? "guest";
            var password = cfg["Password"] ?? "guest";
            var host = cfg["HostName"] ?? "localhost";
            var port = cfg["Port"] ?? "5672";
            var vhost = Uri.EscapeDataString(cfg["VirtualHost"] ?? "/");
            return $"amqp://{user}:{password}@{host}:{port}/{vhost}";
        }
    }
}
