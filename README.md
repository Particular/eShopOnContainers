 # eShopOnContainers with NServiceBus

This is a fork of a [sample .NET Core reference application](https://github.com/dotnet-architecture/eShopOnContainers] developed by Microsoft, modified to run on top of NServiceBus. 

The application is based on a simplified microservices architecture and Docker containers. It is more comprehensive and complex than typical sample applications, in particular, it shows many NServiceBus features working together.

If you want to learn more about microservices/SOA architecture, domain modeling and service decomposition, watch [those selected videos](http://learn-particular.thinkific.com/courses/microservices) from Udi Dahan's Advanced Distributed Systems Design course. 

TODO: generate a dedicated redirect link for the course/consider a different video selection

**We're actively working on this sample. To keep up with the code and documentation updates make sure to [watch this repository](https://help.github.com/articles/watching-repositories/). You can also [star the repository](https://help.github.com/articles/about-stars/) to quickly find it in the future.**

**If you have any comments just open an issue in this repository, your feedback will be greatly appreciated!**


# Recommended resources

- [Selected videos](http://learn-particular.thinkific.com/courses/microservices) from Udi Dahan's Advanced Distributed Systems Design course
- [Put your events on a diet]() Andreas Ohlund's presentation and related [blog post](https://particular.net/blog/putting-your-events-on-a-diet)


# NServiceBus overview

NServiceBus is proven message bus that offers one-way, full-duplex, publish/subscribe, and other patterns on top of transports like RabbitMQ, Azure, MSMQ, and MS SQL Server, as well as long-running and time-bound workflows.

Microsoft's eShopOnContainers solution contains a simple, light-weight `IEventBus` implementation running on top of RabbitMQ. That implementation is only intended for development and testing. In this fork we replaced the test implementation with NServiceBus. 

NServiceBus comes with additional features that you'd have to provide in your event bus implementation, including:

- Failures handling: Messages are automatically retried in case of delivery or processing failures.
- Transactions and consistency: Using [the Outbox feature](https://docs.particular.net/nservicebus/outbox/) with [SQL persistence](https://docs.particular.net/persistence/sql/), we ensure that messages are processed exactly once and we provide consistency between messages and downstream operations without using DTC. Note that we keep our business data in the same database as used by SQL persistence.
- ...
- ...

TODO: think about other things worth highlighting


# Architectural/design considerations

NServiceBus is an opinionated framework, following principles for building distributed systems outlined in Udi Dahan's Advanced Distributed Systems Design course. There are some differences between the reference application and the typical recommended design for NServiceBus projects. We'll highlight them in this section.


## Messages, commands and events

In NServiceBus messages are the basic unit of communication. From the technical perspective are simple POCO objects. The messages that request performing a specific action are called _commands_ and messages informing about the fact that happened are called _events_. 

The simplest way to define a message in a system using NServiceBus is to have it implement one of the marker interfaces (`IMessage`, `ICommand` or `IEvent`). In the eShopOnContainers we instead use unobtrusive mode, which allows us to define messages without forcing a dependency on NServiceBus for message classes.

TODO: include snippet here from the app to show how it's done


## Messages and loose-coupling/system maintainability

To ensure that applications are maintainable and easy to evolve, follow those recommendations when designing messages:

- **Keep message definitions as small as possible** - If your message has dozens of properties it may be an indicate that your services are implicitly coupled. Ideally, the messages exchanged between services should contains just a few properties, mainly Ids. See [the put your events on a diet blog post]() and [presentation]() to learn more.
- **Avoid dependencies in message definitions** - Be careful to not rely on 3-rd party frameworks like EntityFramework or internal services data models. Even if ultimately the message will be a copy of another model class, it's better to duplicate, as it'll be easier to modify it in the future.
- ...

Typically in NServiceBus projects messages are defined in a separate assembly that is then shared between services. That assembly becomes a contract between two or more services. In sample projects we often keep all message definitions in a single assembly for simplicity, however in production code it’s better to have multiple assemblies with just a few message definitions in each, to make systems easier to evolve.

In eShopOnContainers project we used a less popular approach, i.e. the message definitions are duplicated in services. That approach is harder to implement from the technical perspective and more brittle, for example, you won’t get compile-time errors when message definitions in two different services get out of sync. To avoid complicated customizations, we had to ensure messages are using the same namespaces. Unfortunately, that caused conflicts in tests. 

Keep in mind that even though we don’t have an explicit dependencies on the shared library with messages definitions, there still exists coupling between services that need to be able to communicate. Only in this casethe coupling is implicit and thus problems may manifest at later stages, e.g. at runtime. 


## Messages routing

NServiceBus automatically scans all assemblies to find message definitions and handlers for messages. We need to provide some additional routing configuration to ensure NServiceBus knows where to send messages and commands.

TODO: include routing definition code here


# Deployment

## Generating databases

Some of the NServiceBus features, e.g. Outbox, require persistance storage. In this sample we use SQL persistence running on top of MS SQL Server databases.

In a dev/test environments you can rely on built-in installers to automatically create databases and tables. However, in production you should use generated scripts and follow the workflow similar to the one described in the [Installer Workflow](https://docs.particular.net/persistence/sql/installer-workflow) article.