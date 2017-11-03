using System;
using System.Threading.Tasks;
using eShopOnContainers.Services.IntegrationEvents.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
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
            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
            var order = new Order(message.UserId, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            foreach (var item in message.Basket.Items)
            {
                var productId = int.TryParse(item.ProductId, out int id) ? id : -1;
                var discount = 0;

                order.AddOrderItem(productId, item.ProductName, item.UnitPrice, discount, item.PictureUrl, item.Quantity);
            }
            _orderRepository.Add(order);

            await _orderRepository.UnitOfWork.SaveEntitiesAsync();

            // We'll use this to uniquely identify the order for orchestrating the grace period.
            var orderId = order.Id;

            // Send Integration event to clean basket once basket is converted to Order and before starting with the order creation process
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId, orderId);
            await context.Publish(orderStartedIntegrationEvent);

            _logger.CreateLogger(nameof(UserCheckoutAcceptedIntegrationEventHandler))
                .LogTrace("UserCheckoutAccepted integration event has been received and a create new order process is started with requestId: {message.RequestId}");
        }        
    }
}