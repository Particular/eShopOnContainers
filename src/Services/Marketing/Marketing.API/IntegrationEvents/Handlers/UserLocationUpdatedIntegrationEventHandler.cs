using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Microsoft.eShopOnContainers.Services.Marketing.API.IntegrationEvents.Handlers
{
    using Marketing.API.Model;
    using Microsoft.eShopOnContainers.Services.Marketing.API.Infrastructure.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class UserLocationUpdatedIntegrationEventHandler 
        : IHandleMessages<UserLocationUpdatedIntegrationEvent>
    {
        private readonly IMarketingDataRepository _marketingDataRepository;

        public UserLocationUpdatedIntegrationEventHandler(IMarketingDataRepository repository)
        {
            _marketingDataRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task Handle(UserLocationUpdatedIntegrationEvent message, IMessageHandlerContext context)
        {
            var userMarketingData = await _marketingDataRepository.GetAsync(message.UserId);
            userMarketingData = userMarketingData ?? 
                new MarketingData() { UserId = message.UserId };

            userMarketingData.Locations = MapUpdatedUserLocations(message.LocationList);
            await _marketingDataRepository.UpdateLocationAsync(userMarketingData);
        }

        private List<Location> MapUpdatedUserLocations(List<UserLocationDetails> newUserLocations)
        {
            var result = new List<Location>();
            newUserLocations.ForEach(location => {
                result.Add(new Location()
                {
                    LocationId = location.LocationId,
                    Code = location.Code,
                    Description = location.Description
                });
            });

            return result;
        }
    }
}
