# The *Ordering* Business Process

There are several steps in the ordering process once the order has been placed, including:

- Verify the buyer's information
- Check for adequate stock
- Wait for a grace period to allow the user time to cancel the order
- Validate the payment

We've implemented this process as an [NServiceBus Saga](https://docs.particular.net/nservicebus/sagas/) primarily because the grace period could be anywhere from a few seconds to several days depending on how it is configured. Here we provide details on how we handle the grace period in particular.

## The Grace Period

This is an implementation of a well known issue in eCommerce, called [buyers remorse](https://en.wikipedia.org/wiki/Buyer%27s_remorse). In short it means that minutes after making a purchase, some customers regret their purchase and decide they want to cancel the order. Instead of immediately processing a purchase, an eCommerce website decides to wait a little and give customers the opportunity to cancel their order. The benefit for eCommerce websites is that processes like shipping a product don't go into effect until we're more sure that customer didn't regret their purchase.

### Original implementation

After publishing the event that the order was purchased, the service responsible for the order implemented the grace period by first storing the order in the database. A process running in the background would query the database for orders where the grace period expired and publish a new event to continue the ordering process. (Note: a background process is often a strong indication that it can be done more effectively with a saga.)

##### Technical implementation

An incoming `UserCheckoutAcceptedIntegrationEvent` arrives in the `Ordering` service. The handler for this event then sends a `CreateOrderCommand` which stores the order in the database.

The `GracePeriodManagerService` is the ASP.NET Core background process (it inherits `HostedService`, which has [hardly any documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice?view=aspnetcore-2.0) at the time of writing) which queries the database and which will publish the `GracePeriodConfirmedIntegrationEvent`.

### New implementation

The new implementation is using an [NServiceBus Saga](https://docs.particular.net/nservicebus/sagas/). This completely replaces the need for a background process querying the database. The saga is able to schedule a message to later continue the process. These messages are called [timeout messages](https://docs.particular.net/nservicebus/sagas/timeouts) and are deferred messages that arrive at a scheduled time. However NServiceBus removes the plumbing and optimizes querying the database to reduce queries. It's also very flexible to use multiple timeout messages which have different intent and can contain data. In this example we only use one timeout called `GracePeriodExpired`.

### Constraints

Before starting to change the code to support NServiceBus sagas, we set up some constraints:

- The GracePeriod saga should orchestrate the business process
- Domain events should be removed in favor of messages
- ServiceInsight should be able to visualize the entire process

##### GracePeriod saga should orchestrate the business process

The original code contained a large number of 'integration events', 'domain events' and 'commands' that basically did one of the following actions.

1. Store data into the database
2. Verified a certain state
3. Publish a new (integration) event

The process itself was scattered throughout the code and pretty hard to follow. With a saga, you'll have a single class that can be triggered by multiple messages. But all the code that deals with the business process itself is inside a single class. This makes it more clear. The state of the business process isn't altered by multiple incoming messages at the same time. NServiceBus takes care of any [concurrency issues](https://docs.particular.net/nservicebus/sagas/concurrency).

Tip: Don't create sagas that handle a large amount of messages. We advise users of NServiceBus to split up sagas either inside a single service, and sometimes over different services. This depends on the service where the data is stored and with which the saga has to work with.

##### Domain events should be removed

The saga is triggered by incoming messages. These can be any (integration) event, but also timeouts that are triggered by the saga itself. Initially the domain events took a large role in this. Because the saga orchestrates the entire process and it is triggered by all incoming messages, the domain events lost their value. The saga literally becomes your domain model.

With the use of the [Outbox feature](https://docs.particular.net/nservicebus/outbox/), both the messages are received and send in the same transaction as the saga state and (possible) the business data, when using handlers and the incoming message context. With domain events that live outside of the context of message handlers, this isn't (easily) possible.

There are still MVC controllers that use domain events.

##### ServiceInsight should be able to visualize the entire process

Within the Particular Software platform, [ServiceInsight](https://docs.particular.net/serviceinsight/) can visualize a running system. It analyzes messages that have been sent and can present views of the message flow. This can be a flow diagram or a sequence diagram and has an additional view for sagas.

In other words, it does not display the flow of messages as we intent to see it. It rather displays the actual messages that were sent by the system and their correlation to each other. Including information like the content of the message and any metrics or exceptions that occurred.

With these diagrams we can verify the working of the system.

##### Additional notes

- The original implementation has integration events with data. We like to keep the saga as clean as possible, but we still need to store the incoming data. That's why we created separate message handlers that store this data. The best example for this is the `UserCheckoutAcceptedIntegrationEvent` that is received by both the saga and the `UserCheckoutAcceptedIntegrationEventHandler`.
- Usually we recommend that the user interface creates a command that stores data, which then fires an event to notify all subscribers. Because we had both incoming integration events and updates from the user interface, we chose to have message handlers and the saga both process the same message. They would both have their own responsibility, a good example of the [Single Responsibility Principle](https://en.wikipedia.org/wiki/Single_responsibility_principle) in action.
- Cancelling the order from the user interface originally worked with domain events. Because the saga orchestrates the cancellation, this was changed into a command message. You can read more on the difference between [commands and events](https://docs.particular.net/nservicebus/messaging/messages-events-commands) in our documentation.
