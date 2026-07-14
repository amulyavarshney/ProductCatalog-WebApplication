namespace BookQuery.Service.Models
{
    public class ProcessedMessage
    {
        public Guid MessageId { get; set; }
        public DateTime ProcessedOnUtc { get; set; }
    }
}
