using System.Configuration;
using Akka.Actor;
using Microsoft.WindowsAzure.Storage;
using MusicIndexer.Messages;
using Serilog;

namespace MusicIndexer.Actors
{
    public class BlobStorageActor : ReceiveActor
    {
        public BlobStorageActor()
        {
            var storageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
            var storageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];
            var storageBlobName = ConfigurationManager.AppSettings["StorageBlobName"];
            var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                storageAccountName, storageAccountKey);

            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(storageBlobName);
            container.CreateIfNotExists();

            Receive<StoreBlobRequest>(
                request =>
                {
                    Log.Information("Storing resource: {resourceUri}", request.StorageLocation);
                    var blockBlob = container.GetBlockBlobReference(request.StorageLocation);
                    if (!blockBlob.Exists())
                    {
                        blockBlob.UploadFromByteArray(request.Data, 0, request.Data.Length);
                    }
                });
        }
    }
}