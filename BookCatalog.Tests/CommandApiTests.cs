using System.Net;
using System.Net.Http.Json;
using BookCatalog.Contracts.Dtos;
using BookCommand.Service.Context;
using BookCommand.Service.MQ;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace BookCatalog.Tests;

public class CommandApiTests : IClassFixture<CommandWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CommandApiTests(CommandWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/book", new BookCreateDto
        {
            Title = "API Book",
            Author = "Tester"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(body);
        Assert.Equal("API Book", body!.Title);
    }

    [Fact]
    public async Task Create_Returns400_WhenTitleMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/book", new { Description = "No title" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns404_WhenMissing()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/book/9999", new BookUpdateDto
        {
            Title = "Missing"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CommandWebApplicationFactory : WebApplicationFactory<BookCommand.Service.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            RemoveDbContext<BookContext>(services);
            services.RemoveAll<IHostedService>();

            services.AddDbContext<BookContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            var rabbitMock = new Mock<IRabbitMQManager>();
            services.RemoveAll<IRabbitMQManager>();
            services.AddSingleton(rabbitMock.Object);
        });
    }

    private static void RemoveDbContext<TContext>(IServiceCollection services) where TContext : DbContext
    {
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(TContext) ||
            d.ServiceType == typeof(DbContextOptions<TContext>) ||
            d.ServiceType == typeof(DbContextOptions) ||
            (d.ImplementationType != null && d.ImplementationType.Name.Contains("DbContext"))).ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}
