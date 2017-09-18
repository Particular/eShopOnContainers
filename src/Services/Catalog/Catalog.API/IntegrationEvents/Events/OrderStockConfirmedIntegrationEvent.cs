namespace eShopOnContainers.Services.IntegrationEvents.Events
{
    public class OrderStockConfirmedIntegrationEvent
    {
        public int OrderId { get; }

        public OrderStockConfirmedIntegrationEvent(int orderId) => OrderId = orderId;
    }
}