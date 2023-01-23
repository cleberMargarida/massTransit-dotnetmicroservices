using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Producer
{
    public class Worker : BackgroundService
    {
        readonly IBus _bus;

        public Worker(IBus bus)
        {
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int count = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                count++;
                await _bus.Publish(new Message { Text = $"The time is {DateTimeOffset.Now}, message Id is {count}" }, stoppingToken);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}