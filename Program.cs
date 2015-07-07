using System;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using MusicIndexer.Processors;
using Serilog;

namespace MusicIndexer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.ColoredConsole().CreateLogger();

            var eventHubConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];
            var eventHubName = ConfigurationManager.AppSettings["EventHubName"];
            var storageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
            var storageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];
            var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                storageAccountName, storageAccountKey);
            var eventProcessorHostName = Guid.NewGuid().ToString();
            var eventProcessorHost = new EventProcessorHost(eventProcessorHostName, eventHubName,
                EventHubConsumerGroup.DefaultGroupName, eventHubConnectionString, storageConnectionString);
            eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>().Wait();
          
            Log.Logger.Information("Receiving. Press enter key to stop worker.");
            Console.ReadLine();
        }
    }
}