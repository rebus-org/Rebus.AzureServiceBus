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

## 7.0.0-a02

* Several adjustments to how queue names are validated and how topic names are generated. Please note that this is a BREAKING CHANGE, because queue names and topic names are no longer automatically lowercased (because it's not necessary), and topic names can now have . in them (because that has always been possible). If you update to 7, you must update ALL of your endpoints, otherwise pub/sub will not work!

[lezzi]: https://github.com/lezzi
[Meyce]: https://github.com/Meyce