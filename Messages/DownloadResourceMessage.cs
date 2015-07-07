using System;

namespace MusicIndexer.Messages
{
    public class DownloadResourceMessage
    {
        public DownloadResourceMessage(Uri resourceUri)
        {
            ResourceUri = resourceUri;
        }


        public Uri ResourceUri { get; private set; }
    }

    public class DownloadMp3 : DownloadResourceMessage
    {
        public DownloadMp3(Uri resourceUri) : base(resourceUri)
        {
        }
    }

    public class DownloadAlbumArt : DownloadResourceMessage
    {
        public DownloadAlbumArt(Uri resourceUri) : base(resourceUri)
        {
        }
    }
}