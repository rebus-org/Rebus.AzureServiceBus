# Changelog

## 2.0.0-a1
* Test release

## 2.0.0-b01
* Test release

## 2.0.0
* Release 2.0.0

## 2.1.0
* Update Azure Service Bus dependency to 3.4.0

## 2.2.0
* Allow `/` in queue and topic names

## 3.0.0
* Update to Rebus 3

## 4.0.0
* Update to Rebus 4
* Update to new project structure (.NET Core unfortunately not support by the driver at this time)

## 4.0.1
* Add .NET Standard 2.0 target specifically to handle dependency on `ConfigurationManager`
* `.ConfigureAwait(false)` everywhere something is `await`ed - thanks [lezzi]

## 5.0.0
* Remove ability to run on the "Basic" tier because it makes the world simpler, and you should at least be using "Standard" anyway
* Add ability to set actual message ID on the `BrokeredMessage` by using Rebus' message ID as the value

## 5.0.1
* Fix handling of connection string when it contains the `EntityPath` element. Makes it possible to use a connection string with only RECEIVE rights to the input queue

## 6.0.0
* Update to Microsoft's new driver and thus gain .NET Core support - finally!
* Add ability configure (and re-configure if possible) these queue settings: partitioning, peek lock duration, default message TTL

## 6.0.1
* Port aforementioned (v. 5.0.1) `EntityPath` handling forward

## 6.0.3
* Fix bug that would result in `MessagingEntityNotFoundException`s when publishing to non-existent topics

## 6.0.4
* Small improvement of subscription registration performance by avoiding an update if the subscription looks as it should

## 6.0.5
* Fix bug that would "forget" to stop automatic peek lock renewal in cases where message handler throws an exception, generating unnecessary noise in the log

## 6.0.6
* Update Azure Service Bus dependency to 3.2.1

## 6.0.7
* Fix bug that would result in always require a manage permission in the shared access policy, even if the queues were already created - thanks [ehabelgindy]

## 7.0.0
* Several adjustments to how queue names are validated and how topic names are generated. Please note that this is a BREAKING CHANGE, because queue names and topic names are no longer automatically lowercased (because it's not necessary), and topic names can now have . in them (because that has always been possible). If you update to 7, you must update ALL of your endpoints, otherwise pub/sub will not work!
* Fix bug that would "forget" to stop automatic peek lock renewal in cases where message handler throws an exception, generating unnecessary noise in the log
* Add ability to run in "legacy naming mode", meaning that topics are more conservatively sanitized to work the same way as all versions of the transport prior to version 7
* Fix bug that accidentally replaced `/` in topic names when publishing, which would cause topics with `/` to be unreachable
* Fix one-way client legacy naming bug (one-way client would not adhere to legacy naming convention, even when `.UseLegacyNaming()` was called on the configuration builder)
* Default to using topics nested beneath their assemblies, so e.g. `await bus.Subscribe<string>()` will result in the creation of a topic named `mscorlib/System.String`, which will be formatted as a topic named `System.String` nested beneath `mscorlib` in tools that support it
* Pluggable naming strategy via `INameFormatter`, allowing for customizing all aspects of how e.g. .NET types are named when creating topics from them, how queue names are normalized/sanitized, etc. - thanks [jr01]
* Added transport setting for overriding the Receive OperationTimeout - thanks [jr01]
* Update Microsoft.Azure.ServiceBus to version 4.1.1 to avoid reconnection bug described here: https://github.com/Azure/azure-service-bus-dotnet/issues/639
* Change it so that topics are initialized by both subscribers and publishers, the philosophy being: If someone subscribes/publishes to it, it must mean that it exists and thus should have an ASB entity representing it
* Ensure that the sequence of operations is bulletproof when renewing peek locks - thanks [jr01]
* Change topic and subscription creation to be more defensive to avoid exceptions as part of normal program flow
* Fix potential deadlock in initialization of topic clients - thanks [jr01]
* Fix bug that prevented one-way clients from deferring messages
* Update to Rebus 6

## 7.1.0
* Add ability to authenticate by prioviding an `ITokenProvider`, enabling much more fine-grained access control - thanks [eeskildsen]

## 7.1.1
* Fix bug that would make the transport unable to receive a message with a NULL value present in the headers dictionary

## 7.1.2
* Make peek lock renewal more robust and less resource-intensive

## 7.1.3
* Update Microsoft.Azure.ServiceBus dependency to 4.1.2

## 7.1.4
* Use transport type from connection string - thanks [benne]

## 7.1.5
* Fix bug that would cause deferred messages to be sent to the wrong queue in cases where a custom queue naming convention was used that would somehow mess with the magic deferred messages queue name

## 7.1.6
* Actually fix bug described in previous entry

## 8.0.0
* Update Microsoft.Azure.ServiceBus dependency to 5.1.0
* Update Microsoft.Identity.Client dependency to 4.22.0

## 8.0.1
* Fix bug that would result in inability to dead-letter a deferred message

## 8.1.0
* Add ability to use native dead-lettering by calling `t => t.UseNativeDeadlettering()` on the transport configurer. The basic of this functionality is the addition of `Message` and `MessageReceiver` to the transaction context, which makes it possible for user code to take over the responsibility for ACK/NACK/Dead-lettering the message

## 8.1.1
* Trim dead letter reason/description to not exceed 4096 characters, which is the maximum length of an ASB header value

## 8.1.5
* Add intelligent batching to send logic, ensuring that request payload stays below 256 kB (and make it configurable!)

## 9.0.0
* Port to new Azure Service Bus driver (Azure.Messaging.ServiceBus) - thanks [binick]
* Use the new driver's built-in ability to create message batches

## 9.0.5
* Update Azure Service Bus driver

## 9.0.6
* Fix bug that would result in creating massive amounts of topic client message senders, which would never be disposed

## 9.0.7
* Log it when using Azure Service Bus' built-in dead-lettering on a message - thanks [hjalle]

## 9.0.8
* Update Azure.Messaging.ServiceBus dependency to 7.5.1

[benne]: https://github.com/benne
[binick]: https://github.com/binick
[eeskildsen]: https://github.com/eeskildsen
[ehabelgindy]: https://github.com/ehabelgindy
[hjalle]: https://github.com/hjalle
[jr01]: https://github.com/jr01
[lezzi]: https://github.com/lezzi
[Meyce]: https://github.com/Meyce
