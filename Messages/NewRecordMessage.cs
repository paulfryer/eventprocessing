using System;

namespace MusicIndexer.Messages
{
    public class NewRecordMessage
    {
        public NewRecordMessage(string artist, string album, string track, Uri fileLocation, Uri albumImageLocation)
        {
            Artist = artist;
            Album = album;
            Track = track;
            FileLocation = fileLocation;
            AlbumImageLocation = albumImageLocation;
        }


        public string Artist { get; private set; }
        public string Album { get; set; }
        public string Track { get; set; }
        public Uri FileLocation { get; set; }
        public Uri AlbumImageLocation { get; set; }

        //public byte[] AlbumImage { get; set; }
    }
}