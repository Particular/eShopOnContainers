namespace eShopOnContainers.Services.IntegrationEvents.Events
{
    public class OrderCancelledIntegrationEvent
    {
        public int OrderId { get; set; }

        public OrderCancelledIntegrationEvent(int orderId)
        {
            OrderId = orderId;
        }
    }
}