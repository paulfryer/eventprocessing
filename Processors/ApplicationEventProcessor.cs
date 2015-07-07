using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using MusicIndexer.Applications;
using Serilog;

namespace MusicIndexer.Processors
{
    public class ApplicationEventProcessor : IEventProcessor
    {
        private readonly List<IEventProcessingApplication> eventProcessingApplications;


        public ApplicationEventProcessor()
            : this(new List<IEventProcessingApplication>
            {
                new NetworkListenerEventProcessingApplication()
            })
        {
        }

        public ApplicationEventProcessor(List<IEventProcessingApplication> eventProcessingApplications)
        {
            this.eventProcessingApplications = eventProcessingApplications;
        }


        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var message in messages)
            {
                try
                {
                    IEventProcessingApplication eventProcessingApplication;
                    if (TryGetEventProcessingApplication(message, out eventProcessingApplication))
                    {
                        var content = Encoding.UTF8.GetString(message.GetBytes());
                        var properties = message.Properties.ToDictionary(k => k.Key, v => (string) v.Value);
                        await eventProcessingApplication.ProcessEvent(content, properties);
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "Error: {e}");
                }

                await context.CheckpointAsync(message);
            }
        }

        public async Task OpenAsync(PartitionContext context)
        {
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
                await context.CheckpointAsync();
        }

        private bool TryGetEventProcessingApplication(EventData eventData,
            out IEventProcessingApplication eventProcessingApplication)
        {
            eventProcessingApplication = null;
            if (eventData.Properties == null || !eventData.Properties.ContainsKey("ApplicationCode"))
                return false;

            var applicationCode = (string) eventData.Properties["ApplicationCode"];

            if (string.IsNullOrEmpty(applicationCode))
                return false;

            eventProcessingApplication =
                eventProcessingApplications.SingleOrDefault(app => app.ApplicationCode == applicationCode);
            return eventProcessingApplication != null;
        }
    }
}