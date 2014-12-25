using System;
using System.Xml.Serialization;

namespace SmartAudioFileManager
{
    [Serializable]
    [XmlRoot("File")]
    public class XmlFileInfo
    {
        [XmlElement(DataType = "string")]
        public string File { get; set; }

        [XmlAttribute(DataType = "string")]
        public string FilePath { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaTitle { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaExtension { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaTrack { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaAlbum { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaYear { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaArtists { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MediaGenres { get; set; }

        [XmlAttribute(DataType = "string")]
        public string MD5Hash { get; set; }
    }
}