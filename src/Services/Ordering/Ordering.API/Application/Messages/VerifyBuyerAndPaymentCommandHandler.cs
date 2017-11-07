namespace Ordering.API.Application.Messages
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.BuyerAggregate;
    using NServiceBus;

    public class VerifyBuyerAndPaymentCommandHandler : IHandleMessages<VerifyBuyerAndPaymentCommand>
    {
        readonly IBuyerRepository buyerRepository;

        public VerifyBuyerAndPaymentCommandHandler(IBuyerRepository buyerRepository)
        {
            this.buyerRepository = buyerRepository;
        }

        public async Task Handle(VerifyBuyerAndPaymentCommand message, IMessageHandlerContext context)
        {
            var buyer = await buyerRepository.FindAsync(message.UserId);

            var cardTypeId = (message.CardTypeId != 0) ? message.CardTypeId : 1;

            var buyerOriginallyExisted = (buyer != null);
            if (!buyerOriginallyExisted)
            {
                buyer = new Buyer(message.UserId);
            }
            
            buyer.VerifyOrAddPaymentMethod(cardTypeId,
                $"Payment Method on {DateTime.UtcNow}",
                message.CardNumber,
                message.CardSecurityNumber,
                message.CardHolderName,
                message.CardExpiration,
                message.OrderId);

            await buyerRepository.UnitOfWork.SaveEntitiesAsync();
        }
    }
}
