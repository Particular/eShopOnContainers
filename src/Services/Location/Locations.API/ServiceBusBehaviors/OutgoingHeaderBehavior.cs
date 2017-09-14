using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Pipeline;

namespace Microsoft.eShopOnContainers.Services.Locations.API.ServiceBusBehaviors
{
    public class OutgoingHeaderBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            var headers = context.Headers;

            // Remove assembly info from header based on fallback mechanism in NServiceBus
            // https://github.com/Particular/NServiceBus/blob/develop/src/NServiceBus.Core/Unicast/Messages/MessageMetadataRegistry.cs#L55
            var currentType = headers["NServiceBus.EnclosedMessageTypes"];
            var newType = currentType.Substring(0, currentType.IndexOf(','));

            headers["NServiceBus.EnclosedMessageTypes"] = newType;

            return next();
        }
    }
    public class HeaderFeature : Feature
    {
        internal HeaderFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var pipeline = context.Pipeline;
            pipeline.Register<OutgoingHeaderRegistration>();
        }
    }

    public class OutgoingHeaderRegistration : RegisterStep
    {
        public OutgoingHeaderRegistration() : base(
            stepId: "OutgoingHeaderManipulation",
            behavior: typeof(OutgoingHeaderBehavior),
            description: "Remove assembly info from outgoing `NServiceBus.EnclosedMessageTypes` header.")
        {
        }
    }

}
