using BookCatalog.Contracts.Dtos;
using BookCatalog.Contracts.Exceptions;
using BookQuery.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookQuery.Service.Controllers
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
        /// Returns a paged list of books.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<BookDto>>> GetAsync([FromQuery] BookQueryParameters parameters)
        {
            return Ok(await _service.GetAsync(parameters));
        }

        /// <summary>
        /// Returns a single book by id.
        /// </summary>
        [HttpGet("{id:int}", Name = "GetBookById")]
        public async Task<ActionResult<BookDto>> GetByIdAsync(int id)
        {
            try
            {
                return Ok(await _service.GetByIdAsync(id));
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
