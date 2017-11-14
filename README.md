 # eShopOnContainers with NServiceBus

This is a fork of a [sample .NET Core reference application](https://github.com/dotnet-architecture/eShopOnContainers) developed by Microsoft, modified to run on top of NServiceBus. For detailed instructions regarding running the solution, refer to the [Microsoft's how-to](https://github.com/dotnet-architecture/eShopOnContainers/wiki/02.-Setting-eShopOnContainers-in-a-Visual-Studio-2017-environment).

The application is based on a simplified microservices architecture and Docker containers. It is more comprehensive and complex than typical sample applications. In particular, it shows many NServiceBus features working together.

If you want to learn more about microservices/SOA architecture, domain modeling and service decomposition, watch [these selected videos](http://go.particular.net/ADSD-eShopOnContainers) from Udi Dahan's [Advanced Distributed Systems Design](https://particular.net/adsd) course. 

**We're actively working on this sample. To keep up with the code and documentation updates make sure to [watch this repository](https://help.github.com/articles/watching-repositories/). You can also [star the repository](https://help.github.com/articles/about-stars/) to quickly find it in the future.**

**If you have any comments, there are several ways to reach us:**

- Open an issue in this repository
- Chat with an engineer at https://particular.net/ or https://gitter.im/Particular/NServiceBus
- Join a discussion on our [discussion group](https://discuss.particular.net/)

Regardless of how you contact us, your feedback will be greatly appreciated!


## Recommended resources

- [Selected videos](http://go.particular.net/ADSD-eShopOnContainers) from our Advanced Distributed Systems Design course about service design
- [Put your events on a diet](https://skillsmatter.com/skillscasts/2990-events-diet) presentation and the related [blog post](https://particular.net/blog/putting-your-events-on-a-diet)


## NServiceBus overview

NServiceBus is a proven message bus that offers one-way, full-duplex, publish/subscribe, and other patterns on top of transports like RabbitMQ, Azure Service Bus, Azure Storage Queues, Amazon SQS, MSMQ, and MS SQL Server, as well as long-running and time-bound workflows called Sagas.

Microsoft's eShopOnContainers solution contains a simple, light-weight `IEventBus` implementation running on top of RabbitMQ. That implementation is only intended for development and testing. In this fork we replaced the test implementation with NServiceBus. 

NServiceBus comes with additional features that you'd most likely have to provide in your event bus implementation before going into production, including:

#### Automatic error handling

Messages are [automatically retried](https://docs.particular.net/nservicebus/recoverability/) in case of delivery or processing failures.

#### Transactions and consistency

Using [the Outbox feature](https://docs.particular.net/nservicebus/outbox/), we ensure that messages are processed exactly once and we provide consistency between messages and downstream operations without using DTC. Note: Depending on the used RDBMS business data and NServiceBus related data might need to be stored in the same catalog to avoid any attempt to escalate local transactions to distributed ones.

#### Sagas

NServiceBus allows handling of long-running, stateful processes using [Sagas](https://docs.particular.net/nservicebus/sagas/). In eShopOnContainers, the order process is such a process since it must check stock, wait for a simulated payment to complete, and allow for a grace period in which the user can cancel the order.

We have [more information](/readme/graceperiod.md) on how this was implemented.

#### Monitoring and visualization tools

[ServiceInsight](https://docs.particular.net/serviceinsight/) generates visualizations based on runtime information, mainly gathered from messages metadata. That allows for example to see what messages are flowing through the system and which endpoints/services communicate.

![Message flow diagram](/readme/serviceinsight-flowdiagram-01.png)
Diagram showing the flow of messages through the system when an order is placed.

![sequence diagram](/readme/serviceinsight-sequencediagram-01.png)
A sequence diagram depicting the order process.

[ServicePulse](https://docs.particular.net/servicepulse/) comes with a Dashboard for monitoring endpoints, it shows basic statistics regarding rate of processing, information about failed messages, and more. Additionally, it allows for retrying failed messages with a single button click.


## Architectural/design considerations

NServiceBus is an opinionated framework, following principles for building distributed systems outlined in our Advanced Distributed Systems Design course. There are some differences between the original reference application and the typical recommended design for NServiceBus projects. We highlight them in this section.


### Messages, commands and events

In NServiceBus messages are the basic unit of communication. From a technical perspective, they are simple POCO objects. Messages that trigger a specific action are called _commands_ and messages describing something that has happened are called _events_. See the [Conventions](https://docs.particular.net/nservicebus/messaging/conventions) article to learn more.

The simplest way to define a message in a system using NServiceBus is to have it implement one of the marker interfaces (`IMessage`, `ICommand` or `IEvent`). In the eShopOnContainers we use instead [_unobtrusive mode_](https://docs.particular.net/nservicebus/messaging/unobtrusive-mode), which allows us to define messages without forcing a dependency on NServiceBus for message classes.


### Messages and loose-coupling/ensuring system maintainability

To ensure that applications are maintainable and easy to evolve, follow those recommendations when designing messages:

- **Keep message definitions as small as possible:** If your message has dozens of properties it may indicate that your services are implicitly coupled and are not truly autonomous. Ideally, messages exchanged between services should contain just a few properties. In practice most of those properties should be Ids as services should be able to access most of the relevant data locally, without querying other services. Keep in mind that achieving this requires very careful service boundary planning and the process may be counter-intuitive. Often the service boundaries evolve and are adjusted multiple times over time before you find the right design. See our blog post on [putting your events on a diet](https://particular.net/blog/putting-your-events-on-a-diet) and the corresponding [presentation](https://skillsmatter.com/skillscasts/2990-events-diet) to learn more.
- **Avoid dependencies in message definitions:** Follow the Single Responsibility Principle and ensure every message has exactly one purpose. In particular, be careful not to rely on 3rd party frameworks like EntityFramework or internal services data models in message definitions. Even if ultimately the message will be an exact copy of another class, it's generally better to duplicate the code in order to avoid dependencies. In the long run, such messages will be easier to maintain and evolve.

Typically, in NServiceBus projects, messages are defined in separate assemblies that are shared between services. Those assemblies are _contracts_ between two or more services. In sample projects, we often keep all message definitions in a single assembly for simplicity. However, in production code it’s often better to have multiple assemblies with just a few message definitions in each, to make systems easier to evolve.

In the eShopOnContainers project, we used yet another approach with the message definitions replicated across all services. This approach is harder to implement from a technical perspective and it’s more brittle. For example, you won’t get compile-time errors when message definitions in two services get out of sync. To avoid complicated customizations, we had to ensure messages are using the same namespaces. Unfortunately, that caused conflicts in the tests, which now are disabled.

Keep in mind that even though we don’t have an explicit dependency on a shared library containing messages definitions, there is still a coupling between the services that need to communicate with each other. Only in this case, the coupling is implicit, and problems may manifest later, e.g. only at runtime. We plan to address this in the future.

### Messages routing

NServiceBus automatically scans all assemblies to find message definitions and handlers for messages. We need to provide some additional [routing configuration](https://docs.particular.net/nservicebus/messaging/routing) to ensure NServiceBus knows where to send messages and commands.


## Deployment

### Generating databases

Some of the NServiceBus features, e.g. Outbox, require persistent storage. In this sample we use [SQL persistence](https://docs.particular.net/persistence/sql/) running on Microsoft SQL Server.

In a dev/test environment you can rely on built-in NServiceBus installers to automatically create databases and tables. However, in production you should use generated scripts and follow the workflow similar to the one described in the [Installer Workflow](https://docs.particular.net/persistence/sql/installer-workflow) article.

In order to provide a smooth development experience, each NServiceBus endpoint ensures that required database catalog is created before the endpoint starts.
