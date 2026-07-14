using System.ComponentModel.DataAnnotations;

namespace BookCatalog.Contracts.Dtos
{
    public class BookCreateDto
    {
        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Author { get; set; }
    }
}
