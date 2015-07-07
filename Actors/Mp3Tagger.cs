using System;
using Akka.Actor;
using MusicIndexer.Media;
using TagLib;

namespace MusicIndexer.Actors
{
    public class Mp3Tagger : ReceiveActor
    {
        public Mp3Tagger()
        {
            Receive<dynamic>(message =>
            {
                // NOTE: gettting stack overflow here!
                var streamFile = new StreamFile(message.Resource, message.Path);

                var f = File.Create(streamFile);
                f.Tag.Album = message.Album;
                // f.Tag.AlbumArtists = new[] {message.Artist};
                f.Tag.Title = message.Track;


                //f.Tag.Pictures = new IPicture[new Picture()];
                f.Save();

                var buffer = new byte[streamFile.ReadStream.Length];

                streamFile.ReadStream.Read(buffer, 0, Convert.ToInt32(streamFile.ReadStream.Length));
            });
        }
    }
}