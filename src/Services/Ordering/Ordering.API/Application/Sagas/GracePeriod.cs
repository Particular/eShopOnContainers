using System;

namespace Ordering.API.Application.Sagas
{
    using System.Threading.Tasks;
    using eShopOnContainers.Services.IntegrationEvents.Events;
    using Microsoft.eShopOnContainers.Services.Ordering.API;
    using Microsoft.Extensions.Options;
    using NServiceBus;
    using NServiceBus.Persistence.Sql;

    public class GracePeriod : SqlSaga<GracePeriod.GracePeriodState>,
        IAmStartedByMessages<OrderStartedIntegrationEvent>,
        IHandleMessages<OrderStockConfirmedIntegrationEvent>,
        IHandleMessages<OrderStockRejectedIntegrationEvent>,
        IHandleMessages<OrderPaymentSuccededIntegrationEvent>,
        IHandleMessages<OrderPaymentFailedIntegrationEvent>,
        IHandleTimeouts<GracePeriodExpired>
    {
        readonly OrderingSettings settings;

        public GracePeriod(IOptions<OrderingSettings> settings)
        {
            this.settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<OrderStartedIntegrationEvent>(_ => _.OrderId);
        }

        protected override string CorrelationPropertyName => nameof(GracePeriodState.OrderIdentifier);

        public async Task Handle(OrderStartedIntegrationEvent message, IMessageHandlerContext context)
        {
            Data.UserId = message.UserId;

            // We'll do the following actions at the same time, but asynchronous
            // - Grace Period
            // - ~~Verify buyer and payment method~~
            // - Verify if there is stock available
            var @event = new OrderStatusChangedToAwaitingValidationIntegrationEvent(message.OrderId, message.OrderedItems);
            await context.Publish(@event);
            await RequestTimeout<GracePeriodExpired>(context, TimeSpan.FromMinutes(settings.GracePeriodTime));
        }

        public class GracePeriodState : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            public int OrderIdentifier { get; set; }
            public string UserId { get; set; }
            public bool GracePeriodIsOver { get; set; }
            public bool StockConfirmed { get; set; }
        }

        public async Task Timeout(GracePeriodExpired state, IMessageHandlerContext context)
        {
            Data.GracePeriodIsOver = true;
            await ContinueOrderingProcess(context);
        }

        private async Task ContinueOrderingProcess(IMessageHandlerContext context)
        {
            if (Data.GracePeriodIsOver && Data.StockConfirmed)
            {
                // Should this be OrderStatusChangedToStockConfirmedIntegrationEvent ???
                var @event = new GracePeriodConfirmedIntegrationEvent(Data.OrderIdentifier);
                await context.Publish(@event);

                // Should we immediately do this?
                var event2 = new OrderStatusChangedToStockConfirmedIntegrationEvent(Data.OrderIdentifier);
                await context.Publish(event2);
            }
        }

        public async Task Handle(OrderStockConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            Data.StockConfirmed = true;
            await ContinueOrderingProcess(context);
        }

        public Task Handle(OrderStockRejectedIntegrationEvent message, IMessageHandlerContext context)
        {
            // This should probably update the order (ie fire an event) so that the UI is updated that it failed.
            throw new NotImplementedException();
        }

        public Task Handle(OrderPaymentSuccededIntegrationEvent message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        public Task Handle(OrderPaymentFailedIntegrationEvent message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
