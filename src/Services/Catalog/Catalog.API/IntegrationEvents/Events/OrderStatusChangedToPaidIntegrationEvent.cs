namespace eShopOnContainers.Services.IntegrationEvents.Events
{
    using System.Collections.Generic;
    using System.Linq;

    public class OrderStatusChangedToPaidIntegrationEvent
    {
        public int OrderId { get; set; }
        public List<OrderStockItem> OrderStockItems { get; set; }

        public OrderStatusChangedToPaidIntegrationEvent(int orderId,
            IEnumerable<OrderStockItem> orderStockItems)
        {
            OrderId = orderId;
            OrderStockItems = orderStockItems.ToList();
        }
    }
}