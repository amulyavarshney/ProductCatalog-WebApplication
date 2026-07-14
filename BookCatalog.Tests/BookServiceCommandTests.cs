using BookCatalog.Contracts.Dtos;
using BookCommand.Service.Context;
using BookCommand.Service.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookCatalog.Tests;

public class BookServiceCommandTests
{
    private static BookContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BookContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsBookAndOutboxMessage()
    {
        await using var context = CreateContext();
        var service = new BookService(context);

        var result = await service.CreateAsync(new BookCreateDto
        {
            Title = "Clean Code",
            Description = "A handbook",
            Author = "Robert C. Martin"
        });

        Assert.True(result.Id > 0);
        Assert.Equal("Clean Code", result.Title);
        Assert.Single(context.Books);
        Assert.Single(context.OutboxMessages);
        Assert.Contains("\"Command\":0", context.OutboxMessages.Single().Payload);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTitleAndWritesOutbox()
    {
        await using var context = CreateContext();
        var service = new BookService(context);
        var created = await service.CreateAsync(new BookCreateDto
        {
            Title = "Old Title",
            Author = "Author"
        });

        await service.UpdateAsync(created.Id, new BookUpdateDto
        {
            Title = "New Title",
            Description = "Updated",
            Author = "Author"
        });

        var book = await context.Books.SingleAsync();
        Assert.Equal("New Title", book.Title);
        Assert.Equal(2, context.OutboxMessages.Count());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndWritesOutbox()
    {
        await using var context = CreateContext();
        var service = new BookService(context);
        var created = await service.CreateAsync(new BookCreateDto { Title = "To Delete" });

        await service.DeleteAsync(created.Id);

        Assert.Empty(context.Books);
        var deleted = await context.Books.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.Equal(2, context.OutboxMessages.Count());
        Assert.Contains("\"Command\":2", context.OutboxMessages.Last().Payload);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenMissing()
    {
        await using var context = CreateContext();
        var service = new BookService(context);

        await Assert.ThrowsAsync<BookCatalog.Contracts.Exceptions.NotFoundException>(() =>
            service.UpdateAsync(999, new BookUpdateDto { Title = "Nope" }));
    }
}
