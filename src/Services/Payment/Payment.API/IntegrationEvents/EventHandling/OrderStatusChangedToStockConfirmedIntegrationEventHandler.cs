using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Payment.API.IntegrationEvents.EventHandling
{
    using Microsoft.Extensions.Options;
    using System.Threading.Tasks;

    public class OrderStatusChangedToStockConfirmedIntegrationEventHandler : 
        IHandleMessages<OrderStatusChangedToStockConfirmedIntegrationEvent>
    {
        private readonly PaymentSettings _settings;

        public OrderStatusChangedToStockConfirmedIntegrationEventHandler( 
            IOptionsSnapshot<PaymentSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            //Business feature comment:
            // When OrderStatusChangedToStockConfirmed Integration Event is handled.
            // Here we're simulating that we'd be performing the payment against any payment gateway
            // Instead of a real payment we just take the env. var to simulate the payment 
            // The payment can be successful or it can fail

            if (_settings.PaymentSucceded)
            {
                await context.Publish(new OrderPaymentSuccededIntegrationEvent(message.OrderId));
            }
            else
            {
                await context.Publish(new OrderPaymentFailedIntegrationEvent(message.OrderId));
            }
        }       
    }
}