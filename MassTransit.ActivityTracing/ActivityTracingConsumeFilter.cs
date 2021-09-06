using System.Diagnostics;
using System.Threading.Tasks;
using GreenPipes;

namespace MassTransit.ActivityTracing
{
    public class ActivityTracingConsumeFilter : IFilter<ConsumeContext>
    {
        public void Probe(ProbeContext context)
        { }

        public Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            var operationName = $"Consuming Message: {context.DestinationAddress.GetExchangeName()}";

            string? parentActivityId = Activity.Current != null ? Activity.Current.Id : null;

            using (var activity = new Activity(operationName).Start())
            {
                if (!context.Headers.TryGetHeader(Constants.TraceParentHeaderName, out var requestId))
                {
                    context.Headers.TryGetHeader(Constants.RequestIdHeaderName, out requestId);
                }

                if (!string.IsNullOrEmpty(requestId?.ToString()))
                {
                    // This is the magic
                    activity.SetParentId(requestId?.ToString());

                    if (context.Headers.TryGetHeader(Constants.TraceStateHeaderName, out var traceState))
                    {
                        activity.TraceStateString = traceState?.ToString();
                    }
                }

                if (parentActivityId is not null)
                    activity.SetParentId(parentActivityId);

                activity
                    .AddTag("message-types", string.Join(", ", context.SupportedMessageTypes))
                    .AddTag("source-host-masstransit-version", context.Host.MassTransitVersion)
                    .AddTag("source-host-process-id", context.Host.ProcessId)
                    .AddTag("source-host-framework-version", context.Host.FrameworkVersion)
                    .AddTag("source-host-machine", context.Host.MachineName)
                    .AddTag("input-address", context.ReceiveContext.InputAddress.ToString())
                    .AddTag("destination-address", context.DestinationAddress?.ToString())
                    .AddTag("source-address", context.SourceAddress?.ToString())
                    .AddTag("initiator-id", context.InitiatorId?.ToString())
                    .AddTag("message-id", context.MessageId?.ToString());

                return next.Send(context);
            }
        }      
    }
}
