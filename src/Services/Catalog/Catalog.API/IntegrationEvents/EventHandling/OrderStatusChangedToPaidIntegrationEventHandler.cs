using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents.EventHandling
{
    using System.Threading.Tasks;
    using Infrastructure;

    public class OrderStatusChangedToPaidIntegrationEventHandler : 
        IHandleMessages<OrderStatusChangedToPaidIntegrationEvent>
    {
        private readonly CatalogContext _catalogContext;

        public OrderStatusChangedToPaidIntegrationEventHandler(CatalogContext catalogContext)
        {
            _catalogContext = catalogContext;
        }

        public async Task Handle(OrderStatusChangedToPaidIntegrationEvent message, IMessageHandlerContext context)
        {
            //we're not blocking stock/inventory
            foreach (var orderStockItem in message.OrderStockItems)
            {
                var catalogItem = _catalogContext.CatalogItems.Find(orderStockItem.ProductId);

                catalogItem.RemoveStock(orderStockItem.Units);
            }

            await _catalogContext.SaveChangesAsync();
        }
    }
}