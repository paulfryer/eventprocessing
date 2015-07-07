using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;

namespace MusicIndexer.Applications
{
    public class NetworkListenerEventProcessingApplication : IEventProcessingApplication
    {
        public NetworkListenerEventProcessingApplication()
        {
            EmptyContentReceived += OnEmptyContentReceived;
            StringContentReceived += OnStringContentReceived;
            XmlContentReceived += OnXmlContentReceived;
            PropertiesReceived += OnPropertiesReceived;
            PropertyReceived += OnPropertyReceived;
            LastChangedPropertiesReceived += OnLastChangedPropertiesReceived;
        }


        public string ApplicationCode
        {
            get { return "NetworkListener"; }
        }

        public async Task ProcessEvent(string content, Dictionary<string, string> properties)
        {
            if (content.StartsWith("<") && content.EndsWith(">") && XmlContentReceived != null)
            {
                XmlContentReceived(this, new XmlContent
                {
                    Properties = properties,
                    Xml = XElement.Parse(content)
                });
            }
            else if (!string.IsNullOrEmpty(content) && StringContentReceived != null
                )
                StringContentReceived(this, new StringContent
                {
                    Properties = properties,
                    Content = content
                });
            else if (EmptyContentReceived != null)
                EmptyContentReceived(this, new Content {Properties = properties});
        }

        private void OnLastChangedPropertiesReceived(object sender, LastChangedPropertiesContent e)
        {
            foreach (var p in e.LastChangedProperties
                )
            {
                Log.Logger.Information("=====================================");
                Log.Logger.Information(p.Key + ": " + p.Value);
            }
        }

        private void OnPropertiesReceived(object sender, PropertiesContent e)
        {
            foreach (var changedProperty in e.ChangedProperties.Where(changedProperty => PropertyReceived != null))
                PropertyReceived(this, new PropertyContent
                {
                    Properties = e.Properties,
                    Name = changedProperty.Key,
                    Value = changedProperty.Value
                });
        }

        private void OnPropertyReceived(object sender, PropertyContent e)
        {
            switch (e.Name)
            {
                case "LastChange":

                    var lastChangeXml = XElement.Parse(e.Value);


                    var instanceNode =
                        lastChangeXml.Descendants(XName.Get("InstanceID", "urn:schemas-upnp-org:metadata-1-0/AVT/"))
                            .SingleOrDefault();


                    if (instanceNode != null && LastChangedPropertiesReceived != null)
                    {
                        var lastChangedProperties = new Dictionary<string, string>();
                        foreach (var childNode in instanceNode.Descendants())
                        {
                            var name = childNode.Name.LocalName;
                            var value =
                                childNode.Attribute("val").Value;
                            lastChangedProperties.Add(name, value);
                        }
                        LastChangedPropertiesReceived(this, new LastChangedPropertiesContent
                        {
                            Properties = e.Properties,
                            LastChangedProperties = lastChangedProperties
                        });
                    }
                    break;

                default:
                    Log.Logger.Information("Unhandled property: " + e.Name + ": " + e.Value);
                    break;
            }
        }

        private void OnXmlContentReceived(object sender, XmlContent e)
        {
            var propertyNodes = e.Xml.Descendants(XName.Get("property", "urn:schemas-upnp-org:event-1-0"));
            var changedProperties = propertyNodes
                .Select(propertyNode => propertyNode.FirstNode)
                .OfType<XElement>()
                .ToDictionary(propertyElement => propertyElement.Name.LocalName,
                    propertyElement => propertyElement.Value);
            if (PropertiesReceived != null)
                PropertiesReceived(this, new PropertiesContent
                {
                    Properties = e.Properties,
                    ChangedProperties = changedProperties
                });
        }

        private void OnStringContentReceived(object sender, StringContent e)
        {
            Log.Logger.Information("STRING CONTENT RECEIVED: ");
            foreach (var property in e.Properties)
                Log.Logger.Information("   " + property.Key + ": " + property.Value);
            Log.Logger.Information(e.Content);
        }

        private void OnEmptyContentReceived(object sender, Content e)
        {
            Log.Logger.Information("EMPTY CONTENT RECEIVED: ");
            foreach (var property in e.Properties)
                Log.Logger.Information("   " + property.Key + ": " + property.Value);
        }

        private void LastChangeHandler(XElement lastChangeXml)
        {
            var currentTrackMetaDataNode = lastChangeXml
                .Descendants(XName.Get("CurrentTrackMetaData",
                    "urn:schemas-upnp-org:metadata-1-0/AVT/"))
                .SingleOrDefault(n => !String.IsNullOrEmpty(n.Attribute("val").Value));
            if (currentTrackMetaDataNode != null)
            {
                var metadata = currentTrackMetaDataNode.Attribute(XName.Get("val")).Value;
                var metaXml = XElement.Parse(metadata);
                var classTypeNode =
                    metaXml.Descendants(XName.Get("class", "urn:schemas-upnp-org:metadata-1-0/upnp/"))
                        .SingleOrDefault();
                if (classTypeNode != null)
                {
                    var classType = classTypeNode.Value;
                    switch (classType)
                    {
                        case "object.item.audioItem.musicTrack":
                            var res =
                                metaXml.Descendants(XName.Get("res",
                                    "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"))
                                    .SingleOrDefault();
                            if (res != null)
                            {
                                var urlString = res.Value;
                                var albumNode =
                                    metaXml.Descendants(XName.Get("album",
                                        "urn:schemas-upnp-org:metadata-1-0/upnp/"))
                                        .SingleOrDefault();
                                string album = null;
                                if (albumNode != null)
                                    album = albumNode.Value;
                                var track =
                                    metaXml.Descendants(XName.Get("title",
                                        "http://purl.org/dc/elements/1.1/"))
                                        .Single()
                                        .Value;
                                var artist =
                                    metaXml.Descendants(XName.Get("creator",
                                        "http://purl.org/dc/elements/1.1/"))
                                        .Single()
                                        .Value;
                                urlString = urlString.Replace("pndrradio-", string.Empty);
                                var fileLocation = new Uri(urlString);
                                var albumArtUriNode = metaXml.Descendants(XName.Get("albumArtURI",
                                    "urn:schemas-upnp-org:metadata-1-0/upnp/")).SingleOrDefault();
                                Uri albumArtUri = null;
                                if (albumArtUriNode != null)
                                    albumArtUri = new Uri(albumArtUriNode.Value);
                            }
                            break;
                    }
                }
            }
        }

        public event EventHandler<XmlContent> XmlContentReceived;
        public event EventHandler<StringContent> StringContentReceived;
        public event EventHandler<Content> EmptyContentReceived;
        public event EventHandler<PropertyContent> PropertyReceived;
        public event EventHandler<PropertiesContent> PropertiesReceived;
        public event EventHandler<LastChangedPropertiesContent> LastChangedPropertiesReceived;
    }

    public class LastChangedPropertiesContent : Content
    {
        public Dictionary<string, string> LastChangedProperties { get; set; }
    }

    public class Content
    {
        public Dictionary<string, string> Properties { get; set; }
    }

    public class XmlContent : Content
    {
        public XElement Xml { get; set; }
    }

    public class StringContent : Content
    {
        public string Content { get; set; }
    }


    public class PropertyContent : Content
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class PropertiesContent : Content
    {
        public Dictionary<string, string> ChangedProperties { get; set; }
    }
}