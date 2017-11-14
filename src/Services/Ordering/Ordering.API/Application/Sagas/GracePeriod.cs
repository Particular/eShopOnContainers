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
        IHandleMessages<OrderCancelledIntegrationEvent>,
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
            mapper.ConfigureMapping<OrderStockConfirmedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderStockRejectedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderPaymentSuccededIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderPaymentFailedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderCancelledIntegrationEvent>(_ => _.OrderId);
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

        public async Task Timeout(GracePeriodExpired state, IMessageHandlerContext context)
        {
            Data.GracePeriodIsOver = true;
            await ContinueOrderingProcess(context);
        }

        private async Task ContinueOrderingProcess(IMessageHandlerContext context)
        {
            if (Data.GracePeriodIsOver && Data.StockConfirmed)
            {
                var stockConfirmedEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(Data.OrderIdentifier);
                await context.Publish(stockConfirmedEvent);
            }
        }

        public async Task Handle(OrderStockConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            Data.StockConfirmed = true;
            await ContinueOrderingProcess(context);
        }

        public Task Handle(OrderStockRejectedIntegrationEvent message, IMessageHandlerContext context)
        {
            // Another handler for OrderStockRejectedIntegrationEvent will update the status of the order
            MarkAsComplete();
            return Task.CompletedTask;
        }

        public Task Handle(OrderPaymentSuccededIntegrationEvent message, IMessageHandlerContext context)
        {
            // TODO: Publish this, but perhaps create a saga in stock as wel???
            //new OrderStatusChangedToPaidIntegrationEvent(Data.OrderIdentifier, )

            MarkAsComplete();

            return Task.CompletedTask;
        }

        public Task Handle(OrderPaymentFailedIntegrationEvent message, IMessageHandlerContext context)
        {
            // Another handler for OrderPaymentFailedIntegrationEvent will update the status of the order
            MarkAsComplete();
            return Task.CompletedTask;
        }

        public Task Handle(OrderCancelledIntegrationEvent message, IMessageHandlerContext context)
        {
            // Nothing more to do; the saga is over
            MarkAsComplete();
            return Task.CompletedTask;
        }

        /// <summary>
        /// State for our GracePeriod saga
        /// </summary>
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
    }
}
