namespace eShopOnContainers.Services.IntegrationEvents.Events
{
    using System.Collections.Generic;

    public class OrderCancelledIntegrationEvent
    {
        public int OrderId { get; set; }

        public OrderCancelledIntegrationEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}