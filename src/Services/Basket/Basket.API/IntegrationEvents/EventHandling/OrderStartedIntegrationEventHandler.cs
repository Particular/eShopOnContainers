using Basket.API.IntegrationEvents.Events;
using Microsoft.eShopOnContainers.Services.Basket.API.Model;
using System;
using System.Threading.Tasks;
using NServiceBus;

namespace Basket.API.IntegrationEvents.EventHandling
{
    public class OrderStartedIntegrationEventHandler : IHandleMessages<OrderStartedIntegrationEvent>
    {
        private readonly IBasketRepository _repository;

        public OrderStartedIntegrationEventHandler(IBasketRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task Handle(OrderStartedIntegrationEvent message, IMessageHandlerContext context)
        {
            await _repository.DeleteBasketAsync(message.UserId.ToString());
        }
    }
}



