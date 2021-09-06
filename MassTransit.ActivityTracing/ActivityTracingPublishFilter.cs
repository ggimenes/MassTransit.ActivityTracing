using System.Diagnostics;
using System.Threading.Tasks;
using GreenPipes;
using System.Linq;
using MassTransit.RabbitMqTransport;

namespace MassTransit.ActivityTracing
{
    public class OpenTracingPublishFilter : IFilter<PublishContext>, IFilter<SendContext>
    {
        public async Task Send(SendContext context, IPipe<SendContext> next)
        {
            string? parentActivityId = Activity.Current != null ? Activity.Current.Id : null;

            using (var activity = new Activity($"{Constants.ProducerActivityName}:{context.DestinationAddress?.GetExchangeName()}").Start())
            {
                InjectHeaders(activity, context);

                if (parentActivityId is not null)
                    activity
                    .SetParentId(parentActivityId);
                else
                    throw new System.Exception("current activity null");

                activity
                   .AddTag("destination-address", context.DestinationAddress?.ToString())
                   .AddTag("source-address", context.SourceAddress?.ToString())
                   .AddTag("initiator-id", context.InitiatorId?.ToString())
                   .AddTag("message-id", context.MessageId?.ToString());

                //if (context.TryGetPayload<RabbitMqSendContext>(out var rabbitMqSendContext))
                //   .AddEvent(new ActivityEvent("message-body", default, new ActivityTagsCollection(rabbitMqSendContext.co...ToDictionary())));

                await next.Send(context);
            }
        }

        public void Probe(ProbeContext context)
        { }

        public async Task Send(PublishContext context, IPipe<PublishContext> next)
        {
            string? parentActivityId = Activity.Current != null ? Activity.Current.Id : null;

            using (var activity = new Activity($"{Constants.PublishProducerActivityName}:{context.DestinationAddress?.GetExchangeName()}").Start())
            {
                InjectHeaders(activity, context);

                if (parentActivityId is not null)
                    activity
                    .SetParentId(parentActivityId);
                else
                    throw new System.Exception("current activity null");

                activity
                   .AddTag("destination-address", context.DestinationAddress?.ToString())
                   .AddTag("source-address", context.SourceAddress?.ToString())
                   .AddTag("initiator-id", context.InitiatorId?.ToString())
                   .AddTag("message-id", context.MessageId?.ToString());

                await next.Send(context);
            }

        }

        private static void InjectHeaders(
            Activity activity,
            SendContext context)
        {
            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!context.Headers.TryGetHeader(Constants.TraceParentHeaderName, out _))
                {
                    context.Headers.Set(Constants.TraceParentHeaderName, activity.Id);
                    if (activity.TraceStateString != null)
                    {
                        context.Headers.Set(Constants.TraceStateHeaderName, activity.TraceStateString);
                    }
                }
            }
            else
            {
                if (!context.Headers.TryGetHeader(Constants.RequestIdHeaderName, out _))
                {
                    context.Headers.Set(Constants.RequestIdHeaderName, activity.Id);
                }
            }
        }

        private static Activity StartActivity()
        {
            var activity = new Activity(Constants.ProducerActivityName);

            activity.Start();

            return activity;
        }

    }
}
