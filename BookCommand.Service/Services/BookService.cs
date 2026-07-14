using BookCatalog.Contracts.Commands;
using BookCatalog.Contracts.Dtos;
using BookCatalog.Contracts.Events;
using BookCatalog.Contracts.Exceptions;
using BookCommand.Service.Context;
using BookCommand.Service.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BookCommand.Service.Services
{
    public interface IBookService
    {
        Task<BookDto> CreateAsync(BookCreateDto book);
        Task UpdateAsync(int id, BookUpdateDto book);
        Task DeleteAsync(int id);
    }

    public class BookService : IBookService
    {
        private readonly BookContext _context;

        public BookService(BookContext context)
        {
            _context = context;
        }

        public async Task<BookDto> CreateAsync(BookCreateDto book)
        {
            var bookEntity = new Book
            {
                Title = book.Title,
                Description = book.Description,
                Author = book.Author
            };

            if (_context.Database.IsRelational())
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.Books.AddAsync(bookEntity);
                await _context.SaveChangesAsync();
                EnqueueOutbox(bookEntity, BookCommandType.Create);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await _context.Books.AddAsync(bookEntity);
                await _context.SaveChangesAsync();
                EnqueueOutbox(bookEntity, BookCommandType.Create);
                await _context.SaveChangesAsync();
            }

            return ToDto(bookEntity);
        }

        public async Task UpdateAsync(int id, BookUpdateDto book)
        {
            var bookEntity = await FromId(id);
            bookEntity.Title = book.Title;
            bookEntity.Description = book.Description;
            bookEntity.Author = book.Author;

            EnqueueOutbox(bookEntity, BookCommandType.Update);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var bookEntity = await FromId(id);
            _context.Books.Remove(bookEntity);

            EnqueueOutbox(bookEntity, BookCommandType.Delete);
            await _context.SaveChangesAsync();
        }

        private void EnqueueOutbox(Book book, BookCommandType command)
        {
            var message = new BookEventMessage
            {
                MessageId = Guid.NewGuid(),
                Command = command,
                BookId = book.Id,
                Title = book.Title,
                Description = book.Description,
                Author = book.Author
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = message.MessageId,
                Type = nameof(BookEventMessage),
                Payload = JsonConvert.SerializeObject(message),
                OccurredOnUtc = DateTime.UtcNow
            });
        }

        private async Task<Book> FromId(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(c => c.Id == id);
            if (book is null)
            {
                throw new NotFoundException(nameof(Book), id);
            }

            return book;
        }

        private static BookDto ToDto(Book book) => new()
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            Author = book.Author
        };
    }
}
