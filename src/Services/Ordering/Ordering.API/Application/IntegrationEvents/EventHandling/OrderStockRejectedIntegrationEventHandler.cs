using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;

    public class OrderStockRejectedIntegrationEventHandler : IHandleMessages<OrderStockRejectedIntegrationEvent>
    {
        private readonly IOrderRepository _orderRepository;

        public OrderStockRejectedIntegrationEventHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task Handle(OrderStockRejectedIntegrationEvent message, IMessageHandlerContext context)
        {
            var orderToUpdate = await _orderRepository.GetAsync(message.OrderId);

            var orderStockRejectedItems = message.OrderStockItems
                .FindAll(c => !c.HasStock)
                .Select(c => c.ProductId);

            orderToUpdate.SetCancelledStatusWhenStockIsRejected(orderStockRejectedItems);

            await _orderRepository.UnitOfWork.SaveEntitiesAsync();
        }
    }
}