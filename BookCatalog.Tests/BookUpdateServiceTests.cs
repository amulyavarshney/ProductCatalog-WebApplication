using BookCatalog.Contracts.Commands;
using BookCatalog.Contracts.Events;
using BookQuery.Service.Context;
using BookQuery.Service.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookCatalog.Tests;

public class BookUpdateServiceTests
{
    private static BookContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BookContext(options);
    }

    [Fact]
    public async Task ApplyAsync_Create_IsIdempotent()
    {
        await using var context = CreateContext();
        var service = new BookUpdateService(context);
        var messageId = Guid.NewGuid();
        var message = new BookEventMessage
        {
            MessageId = messageId,
            Command = BookCommandType.Create,
            BookId = 10,
            Title = "DDD",
            Author = "Evans"
        };

        await service.ApplyAsync(message);
        await service.ApplyAsync(message);

        Assert.Single(context.Books);
        Assert.Single(context.ProcessedMessages);
        Assert.Equal("DDD", context.Books.Single().Title);
    }

    [Fact]
    public async Task ApplyAsync_Update_AppliesTitle()
    {
        await using var context = CreateContext();
        var service = new BookUpdateService(context);

        await service.ApplyAsync(new BookEventMessage
        {
            MessageId = Guid.NewGuid(),
            Command = BookCommandType.Create,
            BookId = 1,
            Title = "Old",
            Author = "A"
        });

        await service.ApplyAsync(new BookEventMessage
        {
            MessageId = Guid.NewGuid(),
            Command = BookCommandType.Update,
            BookId = 1,
            Title = "New",
            Description = "Desc",
            Author = "B"
        });

        var book = context.Books.Single();
        Assert.Equal("New", book.Title);
        Assert.Equal("Desc", book.Description);
        Assert.Equal("B", book.Author);
    }

    [Fact]
    public async Task ApplyAsync_Delete_SoftDeletes()
    {
        await using var context = CreateContext();
        var service = new BookUpdateService(context);

        await service.ApplyAsync(new BookEventMessage
        {
            MessageId = Guid.NewGuid(),
            Command = BookCommandType.Create,
            BookId = 5,
            Title = "Gone"
        });

        await service.ApplyAsync(new BookEventMessage
        {
            MessageId = Guid.NewGuid(),
            Command = BookCommandType.Delete,
            BookId = 5
        });

        Assert.Empty(context.Books);
        var deleted = await context.Books.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
    }
}
