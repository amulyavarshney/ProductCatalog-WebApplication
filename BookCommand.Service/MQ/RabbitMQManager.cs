using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace BookCommand.Service.MQ
{
    public interface IRabbitMQManager
    {
        void Publish<T>(T message, string exchangeName, string exchangeType, string routeKey)
            where T : class;
    }

    public class RabbitMQManager : IRabbitMQManager
    {
        private readonly DefaultObjectPool<IModel> _objectPool;
        private readonly ILogger<RabbitMQManager> _logger;

        public RabbitMQManager(IPooledObjectPolicy<IModel> objectPolicy, ILoggerFactory loggerFactory)
        {
            _objectPool = new DefaultObjectPool<IModel>(objectPolicy, Environment.ProcessorCount);
            _logger = loggerFactory.CreateLogger<RabbitMQManager>();
        }

        public void Publish<T>(T message, string exchangeName, string exchangeType, string routeKey) where T : class
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var channel = _objectPool.Get();

            try
            {
                channel.ExchangeDeclare(exchangeName, exchangeType, durable: true, autoDelete: false, arguments: null);

                var sendBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = Guid.NewGuid().ToString();

                channel.BasicPublish(exchangeName, routeKey, properties, sendBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to exchange {Exchange}", exchangeName);
                throw;
            }
            finally
            {
                _objectPool.Return(channel);
            }
        }
    }
}
