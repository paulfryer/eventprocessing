using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Akka.Actor;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using MusicIndexer.Media;
using MusicIndexer.Messages;
using Serilog;
using TagLib;
using File = TagLib.File;

namespace MusicIndexer.Actors
{
    public class Mp3RecordManager : ReceiveActor
    {
        private static readonly string StorageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
        private static readonly string StorageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];
        private static readonly string StorageTableName = ConfigurationManager.AppSettings["StorageTableName"];
        private static readonly string StorageConnectionString =
            string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                StorageAccountName, StorageAccountKey);

        private readonly CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
        private NewRecordMessage newRecord;
        private string path;
        private TrackEntity trackEntity;

        public Mp3RecordManager(IActorRef resourceDownloader, IActorRef resourceStorer)
        {
            Receive<NewRecordMessage>(async message =>
            {
                newRecord = message;
                path = message.Artist + "\\" + message.Album + "\\" + message.Track + ".mp3";
                var pathHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(path));
                var rowKey = message.Album + " - " + message.Track; //BitConverter.ToString(pathHash).Replace("-", "");
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference(StorageTableName);
                trackEntity = new TrackEntity
                {
                    Album = message.Album,
                    AlbumArtUrl = message.AlbumImageLocation.AbsoluteUri,
                    AlbumArtDownloaded = false,
                    Artist = message.Artist,
                    Track = message.Track,
                    TrackDownloaded = false,
                    TrackUrl = message.FileLocation.AbsoluteUri,
                    PartitionKey = message.Artist,
                    RowKey = rowKey,
                    Timestamp = DateTime.UtcNow
                };
                var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                    trackEntity.PartitionKey);
                var rowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, trackEntity.RowKey);
                var finalFilter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);
                var query = new TableQuery<TrackEntity>().Where(finalFilter);


                var entities = table.ExecuteQuery(query, new TableRequestOptions {RetryPolicy = new NoRetry()});
                if (!entities.Any())
                {
                    var insertOperation = TableOperation.Insert(trackEntity);
                    table.Execute(insertOperation);

                    if (message.AlbumImageLocation != null)
                    {
                        var albumArtDownloadedMessage =
                            await
                                resourceDownloader.Ask<AlbumArtDownloaded>(
                                    new DownloadAlbumArt(message.AlbumImageLocation));
                        trackEntity.AlbumImage = albumArtDownloadedMessage.Resource;
                    }
                    var mp3Downloaded =
                        await resourceDownloader.Ask<Mp3Downloaded>(new DownloadMp3(message.FileLocation));

                    Log.Information("Received downloaded MP3. Length: {length}, Location: {resourceUri}", mp3Downloaded.Resource.Length,
                        mp3Downloaded.ResourceUri);
           

                    var memoryStream = new MemoryStream();
                    memoryStream.Write(mp3Downloaded.Resource, 0, mp3Downloaded.Resource.Length);

                    var simpleFile = new SimpleFile(path, memoryStream);
                    var simpleFileAbstraction = new SimpleFileAbstraction(simpleFile);
                    var file = File.Create(simpleFileAbstraction);

                    file.Tag.Composers = new[] {newRecord.Artist};
                    file.Tag.AlbumArtists = new[] {newRecord.Artist};
                    file.Tag.Title = newRecord.Track;
                    file.Tag.Album = newRecord.Album;

                    file.Tag.Pictures = new IPicture[]
                    {
                        new Picture(trackEntity.AlbumImage)
                    };

                    file.Save();
                    var savedFile = ReadToEnd(simpleFile.Stream);
                    resourceStorer.Tell(new StoreBlobRequest(path, savedFile));

                    Log.Information("Creating record: {artist}", message.Artist);
                }
            });

            Receive<Mp3Downloaded>(message =>
            {
                Log.Information("receieved downloaded MP3. Length: {length}, Resource URI: {resourceUri}",
                    message.Resource.Length,
                    message.ResourceUri);



                var memoryStream = new MemoryStream();
                memoryStream.Write(message.Resource, 0, message.Resource.Length);

                var simpleFile = new SimpleFile(path, memoryStream);
                var simpleFileAbstraction = new SimpleFileAbstraction(simpleFile);
                var file = File.Create(simpleFileAbstraction);

                file.Tag.Composers = new[] {newRecord.Artist};
                file.Tag.AlbumArtists = new[] {newRecord.Artist};
                file.Tag.Title = newRecord.Track;
                file.Tag.Album = newRecord.Album;

                file.Tag.Pictures = new IPicture[]
                {
                    new Picture(trackEntity.AlbumImage)
                };

                file.Save();
                var savedFile = ReadToEnd(simpleFile.Stream);
                resourceStorer.Tell(new StoreBlobRequest(path, savedFile));
            });

            Receive<AlbumArtDownloaded>(message =>
            {
                // this is the Album Art Image file.
                if (newRecord.AlbumImageLocation != null &&
                    message.ResourceUri.AbsoluteUri == newRecord.AlbumImageLocation.AbsoluteUri)
                {
                    var path = newRecord.Artist + "\\" + newRecord.Album + ".jpg";
                    resourceStorer.Tell(new StoreBlobRequest(path, message.Resource));
                }
            });
        }

        public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;
            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }
            try
            {
                var readBuffer = new byte[4096];
                var totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead == readBuffer.Length)
                    {
                        var nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            var temp = new byte[readBuffer.Length*2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte) nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }
                var buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                    stream.Position = originalPosition;
            }
        }


        internal class TrackEntity : TableEntity
        {
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Track { get; set; }
            public string TrackUrl { get; set; }
            public string AlbumArtUrl { get; set; }
            public bool TrackDownloaded { get; set; }
            public bool AlbumArtDownloaded { get; set; }

            public byte[] AlbumImage { get; set; }
        }
    }
}