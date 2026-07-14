namespace BookQuery.Service.Models
{
    public class RabbitMQConfig
    {
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "ms-exchange";
        public string ExchangeType { get; set; } = "topic";
        public string Queue { get; set; } = "ms-queue";
        public string RoutingKey { get; set; } = "cqrs";
        public string DeadLetterExchange { get; set; } = "ms-exchange-dlx";
        public string DeadLetterQueue { get; set; } = "ms-queue-dlq";
    }
}
