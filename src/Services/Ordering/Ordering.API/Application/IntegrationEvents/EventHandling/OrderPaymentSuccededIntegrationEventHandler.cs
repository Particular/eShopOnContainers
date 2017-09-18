using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using System.Threading.Tasks;

    public class OrderPaymentSuccededIntegrationEventHandler : 
        IHandleMessages<OrderPaymentSuccededIntegrationEvent>
    {
        private readonly IOrderRepository _orderRepository;

        public OrderPaymentSuccededIntegrationEventHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task Handle(OrderPaymentSuccededIntegrationEvent message, IMessageHandlerContext context)
        {
            var orderToUpdate = await _orderRepository.GetAsync(message.OrderId);

            orderToUpdate.SetPaidStatus();

            await _orderRepository.UnitOfWork.SaveEntitiesAsync();
        }        
    }
}