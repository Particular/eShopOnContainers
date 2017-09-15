using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Ordering.API.Application.IntegrationEvents.Events;
using System.Threading.Tasks;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    public class GracePeriodConfirmedIntegrationEventHandler : IHandleMessages<GracePeriodConfirmedIntegrationEvent>
    {
        private readonly IOrderRepository _orderRepository;

        public GracePeriodConfirmedIntegrationEventHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Event handler which confirms that the grace period
        /// has been completed and order will not initially be cancelled.
        /// Therefore, the order process continues for validation. 
        /// </summary>
        /// <param name="event">       
        /// </param>
        /// <returns></returns>
        public async Task Handle(GracePeriodConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            var orderToUpdate = await _orderRepository.GetAsync(message.OrderId);
            orderToUpdate.SetAwaitingValidationStatus();
            await _orderRepository.UnitOfWork.SaveEntitiesAsync();
        }
        
    }
}
