using System.Net;
using System.Net.Http.Json;
using BookCatalog.Contracts.Dtos;
using BookQuery.Service.Context;
using BookQuery.Service.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BookCatalog.Tests;

public class QueryApiTests : IClassFixture<QueryWebApplicationFactory>
{
    private readonly QueryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public QueryApiTests(QueryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var response = await _client.GetAsync("/api/v1/book/42");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsPagedResults()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookContext>();
            db.Books.AddRange(
                new Book { Id = 1, Title = "Alpha", Author = "A" },
                new Book { Id = 2, Title = "Beta", Author = "B" });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/book?page=1&pageSize=10&sortBy=title");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<BookDto>>();
        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }
}

public class QueryWebApplicationFactory : WebApplicationFactory<BookQuery.Service.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(BookContext) ||
                d.ServiceType == typeof(DbContextOptions<BookContext>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            var hosted = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BookContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
