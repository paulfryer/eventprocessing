using System.IO;
using File = TagLib.File;

namespace MusicIndexer.Media
{
    public class SimpleFileAbstraction : File.IFileAbstraction
    {
        private readonly SimpleFile file;

        public SimpleFileAbstraction(SimpleFile file)
        {
            this.file = file;
        }

        public string Name
        {
            get { return file.Name; }
        }

        public Stream ReadStream
        {
            get { return file.Stream; }
        }

        public Stream WriteStream
        {
            get { return file.Stream; }
        }

        public void CloseStream(Stream stream)
        {
            stream.Position = 0;
        }
    }
}