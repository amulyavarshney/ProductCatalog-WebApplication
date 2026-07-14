using BookCatalog.Contracts.Commands;
using BookCatalog.Contracts.Events;
using BookQuery.Service.Context;
using BookQuery.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace BookQuery.Service.Services
{
    public interface IBookUpdateService
    {
        Task ApplyAsync(BookEventMessage message);
    }

    public class BookUpdateService : IBookUpdateService
    {
        private readonly BookContext _context;

        public BookUpdateService(BookContext context)
        {
            _context = context;
        }

        public async Task ApplyAsync(BookEventMessage message)
        {
            if (message.MessageId != Guid.Empty)
            {
                var alreadyProcessed = await _context.ProcessedMessages
                    .AnyAsync(m => m.MessageId == message.MessageId);
                if (alreadyProcessed)
                {
                    return;
                }
            }

            switch (message.Command)
            {
                case BookCommandType.Create:
                    await UpsertFromMessageAsync(message);
                    break;
                case BookCommandType.Update:
                    await UpsertFromMessageAsync(message);
                    break;
                case BookCommandType.Delete:
                    await SoftDeleteFromMessageAsync(message);
                    break;
            }

            if (message.MessageId != Guid.Empty)
            {
                _context.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = message.MessageId,
                    ProcessedOnUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpsertFromMessageAsync(BookEventMessage message)
        {
            var book = await _context.Books
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == message.BookId);

            if (book is null)
            {
                book = new Book { Id = message.BookId };
                await _context.Books.AddAsync(book);
            }

            book.Title = message.Title ?? string.Empty;
            book.Description = message.Description;
            book.Author = message.Author;
            book.IsDeleted = false;
        }

        private async Task SoftDeleteFromMessageAsync(BookEventMessage message)
        {
            var book = await _context.Books
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == message.BookId);

            if (book is null)
            {
                return;
            }

            book.IsDeleted = true;
        }
    }
}
