namespace Ordering.API.Application.Messages
{
    using System;

    public class VerifyBuyerAndPaymentCommand
    {
        public string UserId { get; set; }
        public int OrderId { get; set; }
        public int CardTypeId { get; set; }
        public string CardNumber { get; set; }
        public string CardSecurityNumber { get; set; }
        public string CardHolderName { get; set; }
        public DateTime CardExpiration { get; set; }
    }
}
