# MassTransit RabbitMQ

MassTransit is an open-source service bus framework for .NET. It allows for the creation of distributed systems using message-based communication. MassTransit provides a way to send and receive messages between different components of a system, allowing for loose coupling and high scalability. This repository include some use cases for MassTransit. 

   * Decoupling of system components for increased flexibility and maintainability
   * Asynchronous and message-based communication between microservices
   * Distributed messaging and event-driven architectures
   * Sending and receiving messages over RabbitMQ.
------------
Solution 
------------
![](/docs/solution-folders.png)

## Producer
The producer was configured to use RabbitMQ as the transport and to register a hosted service to generate messages for each second, in an ASP.NET Core application.

```csharp
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("rabbitmq", "/");
        cfg.ConfigureEndpoints(context);
    });
});

services.AddHostedService<Worker>();
```
```csharp
public class Worker : BackgroundService
{
    readonly IBus _bus;

    public Worker(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        int count = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            count++;
            await _bus.Publish(new Message 
            { Text = $"The time is {DateTimeOffset.Now}, message Id is {count}" }, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```
## Consumer
The consumer was configured to use RabbitMQ as the message transport, adding a consumer of type MessageConsumer and setting a concurrent message limit of 1.

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<MessageConsumer>(c =>
    {
        c.ConcurrentMessageLimit = 1;
    });

    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("rabbitmq", "/");
        cfg.ConfigureEndpoints(context);
    });
});
```

The ```ConcurrentMessageLimit``` property is set to 1, this means that only one instance of the consumer will be able to concurrently process messages. If more messages are received while the instance are busy, the additional messages will be placed in the queue and will be processed by the consumer once one of the instance becomes available.

## Consumer 2 (AnotherConsumer)
The consumer 2 was configured to use RabbitMQ as the message transport, adding a consumer of type ```AnotherMessageConsumer```.

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<AnotherMessageConsumer>();

    x.UsingRabbitMq((context,cfg) =>
    {
        cfg.Host("rabbitmq", "/");
        cfg.ConfigureEndpoints(context);
    });
});
```

## Orchestration
-----------
This Docker Compose file is used to define and run multi-container applications. The file defines 4 services: rabbitmq, consumer, consumer_2(replica), producer, and anotherconsumer.

* producer: This service is running an instance of a custom image called producer. This service is responsible for publishing messages to RabbitMQ.

* consumer and consumer_2: These services are running instances of the same image called consumer. These services are consuming messages from RabbitMQ.

* anotherconsumer: This service is running an instance of a custom image called anotherconsumer. This service is consuming messages from RabbitMQ.

```yml
version: '3.7'

services:
  rabbitmq:
    hostname: rabbitmq
    image: rabbitmq:3-management
    ports:
      - "5672"
      - "15672"

  consumer:
    image: ${DOCKER_REGISTRY-}consumer
    build:
      context: .
      dockerfile: consumer/Dockerfile

  consumer_2:
    image: ${DOCKER_REGISTRY-}consumer
    build:
      context: .
      dockerfile: consumer/Dockerfile

  producer:
    image: ${DOCKER_REGISTRY-}producer
    build:
      context: .
      dockerfile: producer/Dockerfile

  anotherconsumer:
    image: ${DOCKER_REGISTRY-}anotherconsumer
    build:
      context: .
      dockerfile: AnotherConsumer/Dockerfile
```
Design
-----------
The way masstransit is configured, the system will behave as follows.

![](/docs/docker-compose-orchestration.png)

* The publisher will publish every minute messages for the type Message

* RabbitMQ will distribute the messages

* The service of consumer 1, as it is scaled with two instances, and configured with ```ConcurrentMessageLimit``` = 1, will process each one message at a time, see the output:

consumer 1

    ``` 
        Received Text: The time is 01/23/2023 12:37:46 +00:00, message Id is 1
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:48 +00:00, message Id is 3
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:50 +00:00, message Id is 5
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:52 +00:00, message Id is 7
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:54 +00:00, message Id is 9
    ```
consumer 1 replica

    ``` 
        Received Text: The time is 01/23/2023 12:37:47 +00:00, message Id is 2
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:49 +00:00, message Id is 4
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:51 +00:00, message Id is 6
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:53 +00:00, message Id is 8
    info: Consumer.MessageConsumer[0]
        Received Text: The time is 01/23/2023 12:37:55 +00:00, message Id is 10
    ```

* Consumer 2 (AnotherConsumer) will process all messages, see the output:

```
Received Text: The time is 01/23/2023 12:37:50 +00:00, message Id is 1
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:51 +00:00, message Id is 2
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:52 +00:00, message Id is 3
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:53 +00:00, message Id is 4
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:54 +00:00, message Id is 5
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:55 +00:00, message Id is 6
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:56 +00:00, message Id is 7
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:57 +00:00, message Id is 8
info: AnotherConsumer.AnotherMessageConsumer[0]
      Received Text: The time is 01/23/2023 12:37:58 +00:00, message Id is 9
```
