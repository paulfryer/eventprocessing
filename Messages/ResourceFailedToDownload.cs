using System;

namespace MusicIndexer.Messages
{
    public class ResourceFailedToDownload
    {
        public ResourceFailedToDownload(Uri resourceUri)
        {
            ResourceUri = resourceUri;
        }


        public Uri ResourceUri { get; private set; }
    }
}