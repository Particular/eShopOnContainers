using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Application.DomainEventHandlers.OrderStockConfirmed
{
    using MediatR;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using Microsoft.Extensions.Logging;
    using Domain.Events;
    using System;
    using System.Threading.Tasks;
    using Ordering.API.Application.IntegrationEvents;

    public class OrderStatusChangedToStockConfirmedDomainEventHandler
                   : IAsyncNotificationHandler<OrderStatusChangedToStockConfirmedDomainEvent>
    {
        private readonly IEndpointInstance _endpoint;
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _logger;

        public OrderStatusChangedToStockConfirmedDomainEventHandler(
            IEndpointInstance endpoint,
            IOrderRepository orderRepository, ILoggerFactory logger)
        {
            _endpoint = endpoint;
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(OrderStatusChangedToStockConfirmedDomainEvent orderStatusChangedToStockConfirmedDomainEvent)
        {
            _logger.CreateLogger(nameof(OrderStatusChangedToStockConfirmedDomainEventHandler))
                .LogTrace($"Order with Id: {orderStatusChangedToStockConfirmedDomainEvent.OrderId} has been successfully updated with " +
                          $"a status order id: {OrderStatus.StockConfirmed.Id}");

            var orderStatusChangedToStockConfirmedIntegrationEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(orderStatusChangedToStockConfirmedDomainEvent.OrderId);
            await _endpoint.Publish(orderStatusChangedToStockConfirmedIntegrationEvent);
        }
    }  
}