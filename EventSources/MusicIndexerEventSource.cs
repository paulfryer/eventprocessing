using System;
using System.Diagnostics.Tracing;

namespace MusicIndexer.EventSources
{
    public class MusicIndexerEventSource : EventSource
    {
        private static readonly MusicIndexerEventSource musicIndexerEventSource = new MusicIndexerEventSource();

        public static MusicIndexerEventSource Log
        {
            get { return musicIndexerEventSource; }
        }


        [Event(100, Level = EventLevel.Informational, Message = "Storing resource with storage location: {1}")]
        public void StoreBlob(string storageLocation)
        {
            if (IsEnabled())
                WriteEvent(100, storageLocation);
        }

        [Event(110, Level = EventLevel.Informational, Message = "Downloading resource: {1}")]
        public void DownloadingResource(Uri resourceUri)
        {
            if (IsEnabled())
                WriteEvent(110, resourceUri.AbsoluteUri);
        }
    }
}