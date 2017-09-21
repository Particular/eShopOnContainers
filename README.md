 # eShopOnContainers with NServiceBus

This is a fork of a [sample .NET Core reference application](https://github.com/dotnet-architecture/eShopOnContainers] developed by Microsoft, modified to run on top of NServiceBus. 

The application is based on a simplified microservices architecture and Docker containers. It is more comprehensive and complex than typical sample applications, in particular, it shows many NServiceBus features working together.

If you want to learn more about microservices/SOA architecture, domain modeling and service decomposition, watch [those selected videos](http://go.particular.net/ADSD-eShopOnContainers) from Udi Dahan's Advanced Distributed Systems Design course. 

**We're actively working on this sample. To keep up with the code and documentation updates make sure to [watch this repository](https://help.github.com/articles/watching-repositories/). You can also [star the repository](https://help.github.com/articles/about-stars/) to quickly find it in the future.**

**If you have any comments just open an issue in this repository, your feedback will be greatly appreciated!**


## Recommended resources

- [Selected videos](http://go.particular.net/ADSD-eShopOnContainers) from Udi Dahan's Advanced Distributed Systems Design course about service design
- [Put your events on a diet](https://skillsmatter.com/skillscasts/2990-events-diet) Andreas Ohlund's presentation and the related [blog post](https://particular.net/blog/putting-your-events-on-a-diet)


## NServiceBus overview

NServiceBus is a proven message bus that offers one-way, full-duplex, publish/subscribe, and other patterns on top of transports like RabbitMQ, Azure Service Bus, Azure Storage Queues, Amazon SQS, MSMQ, and MS SQL Server, as well as long-running and time-bound workflows called Sagas.

Microsoft's eShopOnContainers solution contains a simple, light-weight `IEventBus` implementation running on top of RabbitMQ. That implementation is only intended for development and testing. In this fork we replaced the test implementation with NServiceBus. 

NServiceBus comes with additional features that you'd most likely have to provide in your event bus implementation before going into production, including:

#### Failures handling

Messages are [automatically retried](https://docs.particular.net/nservicebus/recoverability/) in case of delivery or processing failures.

#### Transactions and consistency

Using [the Outbox feature](https://docs.particular.net/nservicebus/outbox/) along with a persistence, for example in production with [SQL persistence](https://docs.particular.net/persistence/sql/), we ensure that messages are processed exactly once and we provide consistency between messages and downstream operations without using DTC. Note: Depending on the used RDBMS business data and NServiceBus related data might need to be stored in the same catalog to avoid any attempt to escalate local transactions to distributed ones.

#### [Sagas](https://docs.particular.net/nservicebus/sagas/) 

TODO: describe the customer's remorse, include snippet 

#### Monitoring and visualization tools

TODO: get a screenshot from SI/SP showing failed messages and flow of messages in the solution
TODO: write one paragraph explaining how it works


## Architectural/design considerations

NServiceBus is an opinionated framework, following principles for building distributed systems outlined in Udi Dahan's Advanced Distributed Systems Design course. There are some differences between the original reference application and the typical recommended design for NServiceBus projects. We highlight them in this section.


### Messages, commands and events

In NServiceBus messages are the basic unit of communication. From the technical perspective, they are just simple POCO objects. Messages that request performing a specific action are called _commands_ and messages informing about the fact that something has happened are called _events_. See the [Conventions](https://docs.particular.net/nservicebus/messaging/conventions) article to learn more.

The simplest way to define a message in a system using NServiceBus is to have it implement one of the marker interfaces (`IMessage`, `ICommand` or `IEvent`). In the eShopOnContainers we use instead the _unobtrusive mode_, which allows us to define messages without forcing a dependency on NServiceBus for message classes.

TODO: include snippet here from the app to show how it's done


### Messages and loose-coupling/Ensuring system maintainability

To ensure that applications are maintainable and easy to evolve, follow those recommendations when designing messages:

- **Keep message definitions as small as possible** - If your message has dozens of properties it may indicate that your services are implicitly coupled and are not truly autonomous. Ideally, messages exchanged between services should contain just a few properties. In practice most of those properties should be Ids as services should be able to access most of the relevant data locally, without querying other services. Keep in mind that achieving that effect requires a very careful service boundaries planning and the process may be counter-intuitive. Often the service boundaries evolve and are adjusted multiple times over time before you find the right design. See [the put your events on a diet blog post](https://particular.net/blog/putting-your-events-on-a-diet) and [presentation](https://skillsmatter.com/skillscasts/2990-events-diet) to learn more.
- **Avoid dependencies in message definitions** - Follow the Single Responsibility Principle and ensure every message has exactly one purpose. In particular, be careful to not rely on 3-rd party frameworks like EntityFramework or internal services data models in message definitions. Even if ultimately the message will be an exact copy of another class, it's generally better to duplicate the code in order to avoid dependencies. In the long run, such messages will be easier to maintain and evolve.

Typically in NServiceBus projects messages are defined in separate assemblies that are then shared between services. Those assemblies become contracts between two or more services. In sample projects we often keep all message definitions in a single assembly for simplicity, however in production code it’s better to have multiple assemblies with just a few message definitions in each, to make systems easier to evolve.

In eShopOnContainers project we used a less popular approach, i.e. the message definitions are duplicated in services. That approach is harder to implement from the technical perspective and more brittle, for example, you won’t get compile-time errors when message definitions in two different services get out of sync. To avoid complicated customizations, we had to ensure messages are using the same namespaces. Unfortunately, that caused conflicts in tests which now are disabled. 

Keep in mind that even though we don’t have an explicit dependencies on the shared library with messages definitions, there still exists coupling between services that need to be able to communicate. Only in this case the coupling is implicit and thus problems may manifest at later stages, e.g. only at runtime. 


### Messages routing

NServiceBus automatically scans all assemblies to find message definitions and handlers for messages. We need to provide some additional [routing configuration](https://docs.particular.net/nservicebus/messaging/routing) to ensure NServiceBus knows where to send messages and commands.

TODO: include routing definition code here


## Deployment

### Generating databases

Some of the NServiceBus features, e.g. Outbox, require persistance, i.e. a persistent storage. In this sample we use SQL persistence running on top of MS SQL Server databases.

In a dev/test environments you can rely on built-in NServiceBus installers to automatically create databases and tables. However, in production you should use generated scripts and follow the workflow similar to the one described in the [Installer Workflow](https://docs.particular.net/persistence/sql/installer-workflow) article.