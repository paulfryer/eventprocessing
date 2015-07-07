using System;

namespace MusicIndexer.Messages
{
    public class ResourceDownloadedMessage
    {
        public ResourceDownloadedMessage(Uri resourceUri, byte[] resource)
        {
            ResourceUri = resourceUri;
            Resource = resource;
        }


        public Uri ResourceUri { get; private set; }
        public byte[] Resource { get; private set; }
    }

    public class Mp3Downloaded : ResourceDownloadedMessage
    {
        public Mp3Downloaded(Uri resourceUri, byte[] resource) : base(resourceUri, resource)
        {
        }
    }

    public class AlbumArtDownloaded : ResourceDownloadedMessage
    {
        public AlbumArtDownloaded(Uri resourceUri, byte[] resource) : base(resourceUri, resource)
        {
        }
    }
}