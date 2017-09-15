using NServiceBus;

namespace Ordering.API.Application.DomainEventHandlers.OrderGracePeriodConfirmed
{
    using MediatR;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using Microsoft.Extensions.Logging;
    using Domain.Events;
    using System;
    using System.Threading.Tasks;
    using Ordering.API.Application.IntegrationEvents;
    using System.Linq;
    using Ordering.API.Application.IntegrationEvents.Events;

    public class OrderStatusChangedToAwaitingValidationDomainEventHandler
                   : IAsyncNotificationHandler<OrderStatusChangedToAwaitingValidationDomainEvent>
    {
        private readonly IEndpointInstance _endpoint;
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _logger;

        public OrderStatusChangedToAwaitingValidationDomainEventHandler(
            IEndpointInstance endpoint,
            IOrderRepository orderRepository, ILoggerFactory logger)
        {
            _endpoint = endpoint;
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(OrderStatusChangedToAwaitingValidationDomainEvent orderStatusChangedToAwaitingValidationDomainEvent)
        {
            _logger.CreateLogger(nameof(OrderStatusChangedToAwaitingValidationDomainEvent))
                .LogTrace($"Order with Id: {orderStatusChangedToAwaitingValidationDomainEvent.OrderId} has been successfully updated with " +
                          $"a status order id: {OrderStatus.AwaitingValidation.Id}");

            var orderStockList = orderStatusChangedToAwaitingValidationDomainEvent.OrderItems
                .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.GetUnits()));

            var orderStatusChangedToAwaitingValidationIntegrationEvent = new OrderStatusChangedToAwaitingValidationIntegrationEvent(
                orderStatusChangedToAwaitingValidationDomainEvent.OrderId, orderStockList);
            await _endpoint.Publish(orderStatusChangedToAwaitingValidationIntegrationEvent);
        }
    }  
}