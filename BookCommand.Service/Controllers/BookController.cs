using BookCatalog.Contracts.Dtos;
using BookCatalog.Contracts.Exceptions;
using BookCommand.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookCommand.Service.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookService _service;

        public BookController(IBookService service)
        {
            _service = service;
        }

        /// <summary>
        /// Creates a new book.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BookDto>> CreateAsync(BookCreateDto book)
        {
            var created = await _service.CreateAsync(book);
            return Created($"/api/v1/book/{created.Id}", created);
        }

        /// <summary>
        /// Updates an existing book.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync(int id, BookUpdateDto book)
        {
            try
            {
                await _service.UpdateAsync(id, book);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Soft-deletes a book.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = ex.Message
                });
            }
        }
    }
}
