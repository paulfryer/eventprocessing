using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using MusicIndexer.Messages;
using Serilog;

namespace MusicIndexer.Actors
{
    public class ResourceDownloader : ReceiveActor
    {
        // TODO: develop a actor that creates multiple image sizes of album art.


        private readonly HttpClient httpClient = new HttpClient();

        public ResourceDownloader(IActorRef loggingActor)
        {
            Receive<DownloadMp3>(message =>
            {
                var senderClosure = Sender;

                Log.Information("Downloading {resourceUri}", message.ResourceUri);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = message.ResourceUri
                };
                httpClient.SendAsync(request).ContinueWith(req =>
                {
                    var response = req.Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var data = response.Content.ReadAsByteArrayAsync().Result;
                        return new Mp3Downloaded(message.ResourceUri, data);
                    }
                    //return new ResourceDownloadedMessage(message.ResourceUri);
                    return null;
                }, TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(senderClosure);
            });


            Receive<DownloadAlbumArt>(message =>
            {
                var senderClosure = Sender;

                Log.Information("Downloading {resourceUri}", message.ResourceUri);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = message.ResourceUri
                };
                httpClient.SendAsync(request).ContinueWith(req =>
                {
                    var response = req.Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var data = response.Content.ReadAsByteArrayAsync().Result;
                        return new AlbumArtDownloaded(message.ResourceUri, data);
                    }
                    //return new ResourceDownloadedMessage(message.ResourceUri);
                    return null;
                }, TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(senderClosure);
            });
        }

        private void Receive<T1>(Action<DownloadResourceMessage> action, int p)
        {
            throw new NotImplementedException();
        }
    }
}