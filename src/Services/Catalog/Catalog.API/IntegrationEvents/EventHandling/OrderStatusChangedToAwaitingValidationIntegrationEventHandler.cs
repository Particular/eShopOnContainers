using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents.EventHandling
{
    using System.Threading.Tasks;
    using Infrastructure;
    using System.Collections.Generic;
    using System.Linq;

    public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler : 
        IHandleMessages<OrderStatusChangedToAwaitingValidationIntegrationEvent>
    {
        private readonly CatalogContext _catalogContext;

        public OrderStatusChangedToAwaitingValidationIntegrationEventHandler(CatalogContext catalogContext)
        {
            _catalogContext = catalogContext;
        }

        public async Task Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent message, IMessageHandlerContext context)
        {
            var confirmedOrderStockItems = new List<ConfirmedOrderStockItem>();

            foreach (var orderStockItem in message.OrderStockItems)
            {
                var catalogItem = _catalogContext.CatalogItems.Find(orderStockItem.ProductId);
                var hasStock = catalogItem.AvailableStock >= orderStockItem.Units;
                var confirmedOrderStockItem = new ConfirmedOrderStockItem(catalogItem.Id, hasStock);

                confirmedOrderStockItems.Add(confirmedOrderStockItem);
            }

            if (confirmedOrderStockItems.Any(c => !c.HasStock))
            {
                await context.Publish(new OrderStockRejectedIntegrationEvent(message.OrderId, confirmedOrderStockItems));
            }
            else
            {
                await context.Publish(new OrderStockConfirmedIntegrationEvent(message.OrderId));
            }
        }
    }
}