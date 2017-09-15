using System;
using MediatR;
using System.Threading.Tasks;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Ordering.API.Application.IntegrationEvents.Events;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    public class UserCheckoutAcceptedIntegrationEventHandler : IHandleMessages<UserCheckoutAcceptedIntegrationEvent>
    {
        private readonly IEndpointInstance _endpoint;
        private readonly IMediator _mediator;
        private readonly ILoggerFactory _logger;

        public UserCheckoutAcceptedIntegrationEventHandler(IEndpointInstance endpoint,IMediator mediator, ILoggerFactory logger)
        {
            _endpoint = endpoint;
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Integration event handler which starts the create order process
        /// </summary>
        /// <param name="eventMsg">
        /// Integration event message which is sent by the
        /// basket.api once it has successfully process the 
        /// order items.
        /// </param>
        /// <returns></returns>
        public async Task Handle(UserCheckoutAcceptedIntegrationEvent message, IMessageHandlerContext context)
        {
            var result = false;

            // Send Integration event to clean basket once basket is converted to Order and before starting with the order creation process
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
            await _endpoint.Publish(orderStartedIntegrationEvent);

            if (message.RequestId != Guid.Empty)
            {
                var createOrderCommand = new CreateOrderCommand(message.Basket.Items, message.UserId, message.City, message.Street,
                    message.State, message.Country, message.ZipCode,
                    message.CardNumber, message.CardHolderName, message.CardExpiration,
                    message.CardSecurityNumber, message.CardTypeId);

                var requestCreateOrder = new IdentifiedCommand<CreateOrderCommand, bool>(createOrderCommand, message.RequestId);
                result = await _mediator.Send(requestCreateOrder);
            }            

            _logger.CreateLogger(nameof(UserCheckoutAcceptedIntegrationEventHandler))
                .LogTrace(result ? $"UserCheckoutAccepted integration event has been received and a create new order process is started with requestId: {message.RequestId}" : 
                    $"UserCheckoutAccepted integration event has been received but a new order process has failed with requestId: {message.RequestId}");
        }        
    }
}