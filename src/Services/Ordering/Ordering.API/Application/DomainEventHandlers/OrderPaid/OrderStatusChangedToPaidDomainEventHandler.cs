using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Application.DomainEventHandlers.OrderPaid
{
    using MediatR;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using Microsoft.Extensions.Logging;
    using Domain.Events;
    using System;
    using System.Threading.Tasks;
    using Ordering.API.Application.IntegrationEvents;
    using System.Linq;

    public class OrderStatusChangedToPaidDomainEventHandler
                   : IAsyncNotificationHandler<OrderStatusChangedToPaidDomainEvent>
    {
        private readonly IEndpointInstance _endpoint;
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _logger;

        public OrderStatusChangedToPaidDomainEventHandler(
            IEndpointInstance endpoint,
            IOrderRepository orderRepository, ILoggerFactory logger)
        {
            _endpoint = endpoint;
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(OrderStatusChangedToPaidDomainEvent orderStatusChangedToPaidDomainEvent)
        {
            _logger.CreateLogger(nameof(OrderStatusChangedToPaidDomainEventHandler))
                .LogTrace($"Order with Id: {orderStatusChangedToPaidDomainEvent.OrderId} has been successfully updated with " +
                          $"a status order id: {OrderStatus.Paid.Id}");

            var orderStockList = orderStatusChangedToPaidDomainEvent.OrderItems
                .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.GetUnits()));

            var orderStatusChangedToPaidIntegrationEvent = new OrderStatusChangedToPaidIntegrationEvent(orderStatusChangedToPaidDomainEvent.OrderId,
                orderStockList);
            await _endpoint.Publish(orderStatusChangedToPaidIntegrationEvent);
        }
    }  
}