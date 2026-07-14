using BookCatalog.Contracts.Events;
using BookCommand.Service.Context;
using BookCommand.Service.Models;
using BookCommand.Service.MQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BookCommand.Service.Services
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMQManager _rabbitMqManager;
        private readonly RabbitMQConfig _rabbitMqConfig;
        private readonly ILogger<OutboxPublisherService> _logger;

        public OutboxPublisherService(
            IServiceScopeFactory scopeFactory,
            IRabbitMQManager rabbitMqManager,
            IOptions<RabbitMQConfig> rabbitMqOptions,
            ILogger<OutboxPublisherService> logger)
        {
            _scopeFactory = scopeFactory;
            _rabbitMqManager = rabbitMqManager;
            _rabbitMqConfig = rabbitMqOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox publisher loop failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task PublishPendingMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BookContext>();

            var pending = await context.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var outboxMessage in pending)
            {
                try
                {
                    var payload = JsonConvert.DeserializeObject<BookEventMessage>(outboxMessage.Payload)
                        ?? throw new InvalidOperationException("Outbox payload could not be deserialized.");

                    _rabbitMqManager.Publish(
                        payload,
                        _rabbitMqConfig.Exchange,
                        _rabbitMqConfig.ExchangeType,
                        _rabbitMqConfig.RoutingKey);

                    outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
                    outboxMessage.Error = null;
                }
                catch (Exception ex)
                {
                    outboxMessage.RetryCount++;
                    outboxMessage.Error = ex.Message;
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}", outboxMessage.Id);
                }
            }

            if (pending.Count > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
