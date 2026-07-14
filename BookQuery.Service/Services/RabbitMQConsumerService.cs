using System.Text;
using BookCatalog.Contracts.Events;
using BookQuery.Service.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookQuery.Service.Services
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly RabbitMQConfig _rabbitMqConfig;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQConsumerService(
            ILogger<RabbitMQConsumerService> logger,
            IOptions<RabbitMQConfig> options,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _rabbitMqConfig = options.Value;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = RunConsumerLoopAsync(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunConsumerLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    InitializeMessageQueue();
                    var channel = _channel ?? throw new InvalidOperationException("RabbitMQ channel was not initialized.");
                    var consumer = new EventingBasicConsumer(channel);

                    consumer.Received += (_, args) =>
                    {
                        _ = ProcessMessageAsync(args, stoppingToken);
                    };

                    channel.BasicConsume(_rabbitMqConfig.Queue, autoAck: false, consumer);
                    _logger.LogInformation("RabbitMQ consumer started on queue {Queue}", _rabbitMqConfig.Queue);

                    while (!stoppingToken.IsCancellationRequested && (_connection?.IsOpen ?? false))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ consumer failed; retrying in 5 seconds");
                    SafeClose();
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private void SafeClose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch
            {
                // ignored
            }
            finally
            {
                _channel = null;
                _connection = null;
            }
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs args, CancellationToken stoppingToken)
        {
            var channel = _channel;
            if (channel is null)
            {
                return;
            }

            try
            {
                var messageString = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonConvert.DeserializeObject<BookEventMessage>(messageString);
                if (message is null)
                {
                    throw new InvalidOperationException("Message payload could not be deserialized.");
                }

                using var scope = _scopeFactory.CreateScope();
                var updateService = scope.ServiceProvider.GetRequiredService<IBookUpdateService>();
                await updateService.ApplyAsync(message);

                channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process RabbitMQ message. Sending to dead-letter queue.");
                try
                {
                    // requeue: false routes to DLQ when configured on the queue
                    channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "Failed to nack RabbitMQ message");
                }
            }
        }

        private void InitializeMessageQueue()
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqConfig.HostName,
                UserName = _rabbitMqConfig.UserName,
                Password = _rabbitMqConfig.Password,
                VirtualHost = _rabbitMqConfig.VirtualHost,
                Port = _rabbitMqConfig.Port,
                DispatchConsumersAsync = false
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_rabbitMqConfig.DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            _channel.QueueDeclare(_rabbitMqConfig.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_rabbitMqConfig.DeadLetterQueue, _rabbitMqConfig.DeadLetterExchange, routingKey: string.Empty);

            _channel.ExchangeDeclare(_rabbitMqConfig.Exchange, _rabbitMqConfig.ExchangeType, durable: true, autoDelete: false);

            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _rabbitMqConfig.DeadLetterExchange
            };

            _channel.QueueDeclare(_rabbitMqConfig.Queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(_rabbitMqConfig.Queue, _rabbitMqConfig.Exchange, _rabbitMqConfig.RoutingKey);
            _channel.BasicQos(0, 1, false);

            _connection.ConnectionShutdown += (_, _) =>
            {
                _logger.LogInformation("Message queue connection shutting down...");
            };
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch
            {
                // ignored on dispose
            }

            base.Dispose();
        }
    }
}
