using MediatR;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;
using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.eShopOnContainers.Services.Ordering.Infrastructure.Idempotency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.API.Application.Commands
{
    using eShopOnContainers.Services.IntegrationEvents.Events;
    using NServiceBus;

    public class CancelOrderCommandIdentifiedHandler : IdentifierCommandHandler<CancelOrderCommand, bool>
    {
        public CancelOrderCommandIdentifiedHandler(IMediator mediator, IRequestManager requestManager) : base(mediator, requestManager)
        {
        }

        protected override bool CreateResultForDuplicateRequest()
        {
            return true;                // Ignore duplicate requests for processing order.
        }
    }

    public class CancelOrderCommandHandler : IAsyncRequestHandler<CancelOrderCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMessageSession _endpoint;


        public CancelOrderCommandHandler(IOrderRepository orderRepository, IEndpointInstance endpoint)
        {
            _orderRepository = orderRepository;
            this._endpoint = endpoint;
        }

        /// <summary>
        /// Handler which processes the command when customer executes cancel order from app
        /// </summary>
        public async Task<bool> Handle(CancelOrderCommand command)
        {
            var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
            orderToUpdate.SetCancelledStatus();
            var orderCancelledEvent = new OrderCancelledIntegrationEvent(command.OrderNumber);
            await _endpoint.Publish(orderCancelledEvent);
            return await _orderRepository.UnitOfWork.SaveEntitiesAsync();
        }
    }
}
