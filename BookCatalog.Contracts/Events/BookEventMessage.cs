using BookCatalog.Contracts.Commands;

namespace BookCatalog.Contracts.Events
{
    public class BookEventMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public BookCommandType Command { get; set; }
        public int BookId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
    }
}
