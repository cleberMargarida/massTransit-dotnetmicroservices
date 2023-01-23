using System.Threading.Tasks;
using Common;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AnotherConsumer
{
    public class AnotherMessageConsumer :
        IConsumer<Message>
    {
        readonly ILogger<AnotherMessageConsumer> _logger;

        public AnotherMessageConsumer(ILogger<AnotherMessageConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<Message> context)
        {
            _logger.LogInformation("Received Text: {Text}", context.Message.Text);

            return Task.CompletedTask;
        }
    }
}