using System.IO;

namespace MusicIndexer.Media
{
    public class SimpleFile
    {
        public SimpleFile(string Name, Stream Stream)
        {
            this.Name = Name;
            this.Stream = Stream;
        }

        public string Name { get; set; }
        public Stream Stream { get; set; }
    }
}