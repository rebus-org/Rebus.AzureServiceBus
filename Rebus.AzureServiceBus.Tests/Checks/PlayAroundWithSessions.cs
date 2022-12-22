//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using NUnit.Framework;
//using Rebus.Activation;
//using Rebus.AzureServiceBus.Tests.Bugs;
//using Rebus.Bus;
//using Rebus.Config;
//using Rebus.Routing;
//using Rebus.Routing.TypeBased;
//using Rebus.Tests.Contracts;
//using Rebus.Tests.Contracts.Extensions;

//// ReSharper disable AccessToDisposedClosure
//#pragma warning disable CS1998

//namespace Rebus.AzureServiceBus.Tests.Checks;

//[TestFixture]
//public class PlayAroundWithSessions : FixtureBase
//{
//    string _receiverQueueName;
//    string _senderQueueName;

//    protected override void SetUp()
//    {
//        base.SetUp();

//        _receiverQueueName = GetNextQueueName();
//        _senderQueueName = GetNextQueueName();
//    }

//    [TestCase(3, 10)]
//    public async Task CanReceiveOrderedMessages(int sessionCount, int messageCount)
//    {
//        // session IDs in random order
//        var sessionIds = Enumerable.Range(0, sessionCount).InRandomOrder().ToList();

//        // message IDs in increasing order
//        var messageIds = Enumerable.Range(0, messageCount).ToList();

//        // this is how many messages we'll send/receive in total
//        var expectedTotal = sessionCount * messageCount;

//        var receivedMessagesBySession = new ConcurrentDictionary<int, ConcurrentQueue<int>>();
//        var receivedMessages = new ConcurrentQueue<(int SessionId, int MessageId)>();

//        void RegisterReceivedMessage(int sessionId, int messageId)
//        {
//            receivedMessagesBySession.GetOrAdd(sessionId, _ => new ConcurrentQueue<int>()).Enqueue(messageId);
//            receivedMessages.Enqueue((SessionId: sessionId, MessageId: messageId));
//        }

//        using var receiver = CreateBus(
//            inputQueueName: _receiverQueueName,
//            handlers: a => a.Handle<SessionAwareMessage>(async msg => RegisterReceivedMessage(msg.SessionId, msg.MessageId))
//        );

//        using var sender = CreateBus(
//            inputQueueName: _senderQueueName,
//            routing: r => r.TypeBased().Map<SessionAwareMessage>(_receiverQueueName)
//        );

//        var cancellationToken = Using(new CancellationTokenSource(TimeSpan.FromSeconds(60))).Token;

//        await Task.WhenAll(
//            sessionIds
//                .Select(async sessionId =>
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    var messages = messageIds
//                        .Select(messageId => new SessionAwareMessage(sessionId, messageId));

//                    var headers = new Dictionary<string, string> { ["SessionId"] = $"session-{sessionId}" };

//                    // within the session, we send the messages in order
//                    foreach (var message in messages)
//                    {
//                        cancellationToken.ThrowIfCancellationRequested();

//                        await sender.Send(message, headers);
//                    }
//                })
//        );

//        await receivedMessages.WaitUntil(q => q.Count >= expectedTotal, timeoutSeconds: 30);

//        Assert.That(receivedMessages.Count, Is.EqualTo(expectedTotal));
//        Assert.That(receivedMessagesBySession.Values.Sum(q => q.Count), Is.EqualTo(expectedTotal));

//        CollectionAssert.AreEquivalent(sessionIds, receivedMessagesBySession.Keys);

//        foreach (var sessionId in receivedMessagesBySession.Keys)
//        {
//            var receivedMessageIds = receivedMessagesBySession[sessionId].ToList();

//            Assert.That(receivedMessageIds, Is.EqualTo(messageIds),
//                $@"Messages were not received in the right order in session {sessionId}.

//Expected

//    {string.Join(", ", messageIds)}

//but received

//    {string.Join(", ", receivedMessageIds)}");
//        }
//    }

//    record SessionAwareMessage(int SessionId, int MessageId);

//    static IBus CreateBus(string inputQueueName, Action<StandardConfigurer<IRouter>> routing = null, Action<BuiltinHandlerActivator> handlers = null)
//    {
//        var activator = new BuiltinHandlerActivator();

//        handlers?.Invoke(activator);

//        return Configure.With(activator)
//            .Transport(t => t.UseAzureServiceBus(AsbTestConfig.ConnectionString, inputQueueName).EnableSessions())
//            .Routing(r => routing?.Invoke(r))
//            .Options(o =>
//            {
//                // lots of room for non-deterministic ordering 😈
//                o.SetNumberOfWorkers(numberOfWorkers: 3);
//                o.SetMaxParallelism(maxParallelism: 20);
//            })
//            .Start();
//    }

//    string GetNextQueueName()
//    {
//        var queueName = $"session-queue-{Guid.NewGuid():N}";
//        Using(new QueueDeleter(queueName));
//        return queueName;
//    }
//}