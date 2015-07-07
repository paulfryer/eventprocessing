namespace MusicIndexer.Messages
{
    public class StoreBlobRequest
    {
        public StoreBlobRequest(string storageLocation, byte[] data)
        {
            StorageLocation = storageLocation;
            Data = data;
        }

        public string StorageLocation { get; private set; }

        public byte[] Data { get; private set; }
    }
}