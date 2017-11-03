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
            await RequestTimeout<GracePeriodExpired>(context, TimeSpan.FromMinutes(settings.GracePeriodTime));
        }

        public class GracePeriodState : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            public int OrderIdentifier { get; set; }
        }

        public async Task Timeout(GracePeriodExpired state, IMessageHandlerContext context)
        {
            var @event = new GracePeriodConfirmedIntegrationEvent(Data.OrderIdentifier);
            await context.Publish(@event);

            MarkAsComplete();
        }

        // OrderStatusChangedToStockConfirmedIntegrationEvent

        public Task Handle(OrderStockConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        public Task Handle(OrderStockRejectedIntegrationEvent message, IMessageHandlerContext context)
        {
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
