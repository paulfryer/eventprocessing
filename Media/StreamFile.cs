using System.IO;
using File = TagLib.File;

namespace MusicIndexer.Media
{
    public class StreamFile : File.IFileAbstraction
    {
        private readonly string name;
        private readonly Stream stream;

        public StreamFile(byte[] resource, string name)
        {
            this.stream = new MemoryStream(resource);
            this.name = name;
        }

        public void CloseStream(Stream stream1)
        {
            stream1.Close();
        }

        public string Name
        {
            get { return this.name; }
        }

        public Stream ReadStream
        {
            get { return this.stream; }
        }

        public Stream WriteStream
        {
            get { return stream; }
        }
    }
}