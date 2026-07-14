using BookCatalog.Contracts.Dtos;
using BookCatalog.Contracts.Exceptions;
using BookQuery.Service.Context;
using BookQuery.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace BookQuery.Service.Services
{
    public interface IBookService
    {
        Task<PagedResult<BookDto>> GetAsync(BookQueryParameters parameters);
        Task<BookDto> GetByIdAsync(int id);
    }

    public class BookService : IBookService
    {
        private readonly BookContext _context;

        public BookService(BookContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<BookDto>> GetAsync(BookQueryParameters parameters)
        {
            var page = parameters.Page < 1 ? 1 : parameters.Page;
            var query = _context.Books.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var search = parameters.Search.Trim();
                query = query.Where(b =>
                    b.Title.Contains(search) ||
                    (b.Description != null && b.Description.Contains(search)) ||
                    (b.Author != null && b.Author.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Author))
            {
                var author = parameters.Author.Trim();
                query = query.Where(b => b.Author != null && b.Author.Contains(author));
            }

            query = ApplySort(query, parameters.SortBy, parameters.SortDir);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    Author = b.Author
                })
                .ToListAsync();

            return new PagedResult<BookDto>
            {
                Items = items,
                Page = page,
                PageSize = parameters.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<BookDto> GetByIdAsync(int id)
        {
            var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (book is null)
            {
                throw new NotFoundException(nameof(Book), id);
            }

            return ToDto(book);
        }

        private static IQueryable<Book> ApplySort(IQueryable<Book> query, string sortBy, string sortDir)
        {
            var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLowerInvariant()) switch
            {
                "author" => descending ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
                "id" => descending ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
                _ => descending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
            };
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
