using System;
using System.Threading.Tasks;
using eShopOnContainers.Services.IntegrationEvents.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using System.Collections.Generic;
    using Messages;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;

    public class UserCheckoutAcceptedIntegrationEventHandler : IHandleMessages<UserCheckoutAcceptedIntegrationEvent>
    {
        private readonly ILoggerFactory _logger;
        private readonly IOrderRepository _orderRepository;

        public UserCheckoutAcceptedIntegrationEventHandler(IOrderRepository orderRepository, ILoggerFactory logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Integration event handler which starts the create order process
        /// </summary>
        /// <param name="message">
        /// Integration event message which is sent by the
        /// basket.api once it has successfully process the 
        /// order items.
        /// </param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(UserCheckoutAcceptedIntegrationEvent message, IMessageHandlerContext context)
        {
            // Store immediately in database
            // Normally we'd do a command, store the data into the database and fire an event.
            // This event would then start the saga. Instead, we first store the data from the other servce.
            // Then we fire a new event to start the saga.
            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
            var order = new Order(message.UserId, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            var allItems = new List<OrderStockItem>();
            foreach (var item in message.Basket.Items)
            {
                var productId = int.TryParse(item.ProductId, out int id) ? id : -1;
                var discount = 0;

                order.AddOrderItem(productId, item.ProductName, item.UnitPrice, discount, item.PictureUrl, item.Quantity);

                allItems.Add(new OrderStockItem(productId, item.Quantity));
            }
            _orderRepository.Add(order);

            await _orderRepository.UnitOfWork.SaveEntitiesAsync();

            // We'll use this to uniquely identify the order for orchestrating the grace period.
            var orderId = order.Id;

            // Instead of a domain event, we'll use an asynchronous message
            var cmd = new VerifyBuyerAndPaymentCommand()
            {
                OrderId = orderId,
                UserId = message.UserId,
                CardTypeId = message.CardTypeId,
                CardNumber = message.CardNumber,
                CardSecurityNumber = message.CardSecurityNumber,
                CardHolderName = message.CardHolderName,
                CardExpiration = message.CardExpiration,
            };
            await context.SendLocal(cmd);

            // Send Integration event to clean basket once basket is converted to Order and before starting with the order creation process
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId, orderId, allItems);
            await context.Publish(orderStartedIntegrationEvent);

            _logger.CreateLogger(nameof(UserCheckoutAcceptedIntegrationEventHandler))
                .LogTrace("UserCheckoutAccepted integration event has been received and a create new order process is started with requestId: {message.RequestId}");
        }        
    }
}