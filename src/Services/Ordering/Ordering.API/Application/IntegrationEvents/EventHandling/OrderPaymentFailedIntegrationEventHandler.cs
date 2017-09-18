using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using System.Threading.Tasks;

    public class OrderPaymentFailedIntegrationEventHandler : 
        IHandleMessages<OrderPaymentFailedIntegrationEvent>
    {
        private readonly IOrderRepository _orderRepository;

        public OrderPaymentFailedIntegrationEventHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task Handle(OrderPaymentFailedIntegrationEvent message, IMessageHandlerContext context)
        {
            var orderToUpdate = await _orderRepository.GetAsync(message.OrderId);

            orderToUpdate.SetCancelledStatus();

            await _orderRepository.UnitOfWork.SaveEntitiesAsync();
        }
    }
}
