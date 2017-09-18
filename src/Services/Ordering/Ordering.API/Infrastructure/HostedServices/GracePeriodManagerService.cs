using eShopOnContainers.Services.IntegrationEvents.Events;
using NServiceBus;

namespace Ordering.API.Infrastructure.HostedServices
{
    using Dapper;
    using Microsoft.eShopOnContainers.Services.Ordering.API;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;

    public class GracePeriodManagerService
        : HostedService
    {
        private readonly IEndpointInstance _endpoint;
        private readonly OrderingSettings _settings;
        private readonly ILogger<GracePeriodManagerService> _logger;

        public GracePeriodManagerService(IOptions<OrderingSettings> settings,
            IEndpointInstance endpoint,
            ILogger<GracePeriodManagerService> logger)
        {
            _endpoint = endpoint;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                CheckConfirmedGracePeriodOrders();

                await Task.Delay(_settings.CheckUpdateTime, cancellationToken);

                continue;
            }

            await Task.CompletedTask;
        }

        private void CheckConfirmedGracePeriodOrders()
        {
            _logger.LogDebug($"Checking confirmed grace period orders");

            var orderIds = GetConfirmedGracePeriodOrders();

            foreach (var orderId in orderIds)
            {
                var confirmGracePeriodEvent = new GracePeriodConfirmedIntegrationEvent(orderId);
                _endpoint.Publish(confirmGracePeriodEvent).GetAwaiter().GetResult();
            }
        }

        private IEnumerable<int> GetConfirmedGracePeriodOrders()
        {
            IEnumerable<int> orderIds = new List<int>();

            using (var conn = new SqlConnection(_settings.ConnectionString))
            {
                try
                {
                    conn.Open();
                    orderIds = conn.Query<int>(
                        @"SELECT Id FROM [Microsoft.eShopOnContainers.Services.OrderingDb].[ordering].[orders] 
                            WHERE DATEDIFF(minute, [OrderDate], GETDATE()) >= @GracePeriodTime
                            AND [OrderStatusId] = 1",
                        new { GracePeriodTime = _settings.GracePeriodTime });
                }
                catch (SqlException exception)
                {
                    _logger.LogCritical($"FATAL ERROR: Database connections could not be opened: {exception.Message}");
                }

            }

            return orderIds;
        }
    }
}
