using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicIndexer.Applications
{
    public interface IEventProcessingApplication
    {
        string ApplicationCode { get; }
        Task ProcessEvent(string content, Dictionary<string, string> properties);
    }
}