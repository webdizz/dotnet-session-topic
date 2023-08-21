using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Messaging.Settings;
using Azure.Messaging.ServiceBus;
using System.Diagnostics;

var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

var asbConnectionSettings = new AsbConnectionSettings();
config.Bind("AsbConnectionSettings", asbConnectionSettings);

var asbConsumerClient = new ServiceBusClient(asbConnectionSettings.Topic.Connection.Listen);

var cancellationToken = new CancellationToken();

Task[] tasks = new Task[4];
tasks[0] = Task.Run(() => startReceiver(asbConnectionSettings, asbConsumerClient, cancellationToken, "a"));
tasks[1] = Task.Run(() => startReceiver(asbConnectionSettings, asbConsumerClient, cancellationToken, "b"));
tasks[2] = Task.Run(() => startReceiver(asbConnectionSettings, asbConsumerClient, cancellationToken, "c"));
tasks[3] = Task.Run(() => startReceiver(asbConnectionSettings, asbConsumerClient, cancellationToken, "d"));
await Task.WhenAll(tasks);


static async Task startReceiver(AsbConnectionSettings asbConnectionSettings, ServiceBusClient asbConsumerClient, CancellationToken cancellationToken, String id)
{
    var sessionsCounter = 0;
    while (true)
    {
        await using var sessionReceiver = await asbConsumerClient.AcceptNextSessionAsync(asbConnectionSettings.Topic.Name, asbConnectionSettings.Topic.Subscription, new ServiceBusSessionReceiverOptions
        {
            Identifier = Thread.CurrentThread.ManagedThreadId.ToString(),
            PrefetchCount = 10,
        }, cancellationToken);

        while (true)
        {
            var sessionStopWatch = new Stopwatch();
            sessionStopWatch.Start();
            sessionsCounter++;
            var messages = await sessionReceiver.ReceiveMessagesAsync(maxMessages: 20, maxWaitTime: TimeSpan.FromSeconds(1));
            Console.WriteLine($"Received: {messages.Count} messages SessionId={sessionReceiver.SessionId} ThreadId={Thread.CurrentThread.ManagedThreadId} SessionsCounter={id}{sessionsCounter}");
            if (messages == null || messages.Count == 0)
            {
                break; // No more messages in the session, exit loop
            }

            foreach (var message in messages)
            {
                Console.WriteLine($"Processed: {message.Body} SessionId={message.SessionId} ThreadId={Thread.CurrentThread.ManagedThreadId} SessionsCounter={id}{sessionsCounter}");
                await sessionReceiver.CompleteMessageAsync(message);
            }
            sessionStopWatch.Stop();
            Console.WriteLine($"==SessionId={sessionReceiver.SessionId} ThreadId={Thread.CurrentThread.ManagedThreadId} SessionsCounter={id}{sessionsCounter} Duration={sessionStopWatch.ElapsedMilliseconds}");
        }
    }
}