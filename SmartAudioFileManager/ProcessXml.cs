using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Reflection;

namespace SmartAudioFileManager
{
    internal static class ProcessXml
    {
        #region Variables

        private static List<XmlFileInfo> xfInfoList = new List<XmlFileInfo>();

        #endregion

        #region Getters/Setters

        public static List<XmlFileInfo> XFInfoList
        {
            get { return xfInfoList; }
            set { xfInfoList = value; }
        }

        #endregion

        #region Calculate folder size

        private static long DirectorySize(DirectoryInfo dInfo, string[] fileExtensions)
        {
            long totalSize = dInfo.EnumerateFiles().Where(file => fileExtensions.Contains(file.Extension.ToLower())).Sum(file => file.Length);

            totalSize += dInfo.EnumerateDirectories().Sum(dir => DirectorySize(dir, fileExtensions));

            return totalSize;
        }

        #endregion

        #region Process Xml

        // TODO: interesting to use Reflexion here to define a list of dynamics paramaters
        internal static void CollectXmlFileInfo(ref XElement xNode, FileInfo file)
        {
            using (var filetag = TagLib.File.Create(file.FullName))
            {
                XElement XEl = (new XElement("File", //File node
                         new XAttribute("FilePath", file.FullName), //file path including the file name
                         new XAttribute("MediaTitle", Utils.CleanString(filetag.Tag.Title ?? file.Name)),//the title of the song
                         new XAttribute("MediaExtension", file.Extension), // the extension of the file
                         new XAttribute("MediaTrack", filetag.Tag.Track != 0 ? filetag.Tag.Track.ToString("D2") : Utils.FormatStringBasedOnRegex(file.Name, @"\d+", '0')), //the track number
                         new XAttribute("MediaAlbum", Utils.CleanString(filetag.Tag.Album ?? "Undefined")), // the album of the song
                         new XAttribute("MediaYear", filetag.Tag.Year != 0 ? filetag.Tag.Year.ToString() : "Undefined"), // the year of the song
                         new XAttribute("MediaArtists", Utils.CleanString(string.Join(",", filetag.Tag.Performers ?? filetag.Tag.AlbumArtists) ?? "Undefined")), // the artists of the song
                         new XAttribute("MediaGenres", string.Join(",", filetag.Tag.Genres) ?? "Undefined"),
                         new XAttribute("MD5Hash", Utils.ComputeMD5Hash(file.FullName)) // MD5 Hash of the file
                               ));

                //add a thread to process the Serialization
                XFInfoList.Add(FromXElement(XEl));

                xNode.Add(XEl);
            }
        }

        //Serialization to a XmlFIleInfo object based on a XElement
        private static XmlFileInfo FromXElement(XElement xElement)
        {
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xElement.ToString())))
            {
                var xmlSerializer = new XmlSerializer(typeof(XmlFileInfo));
                return (XmlFileInfo)xmlSerializer.Deserialize(memoryStream);
            }
        }

        //Gather file informations and exclude corrupted files
        private static void ComputeFileInfo(FileInfo[] fList, ref XElement xNode, List<FileInfo> fileEx, List<FileInfo> alreadyProcessedFiles, string[] fileExtensions)
        {
            foreach (var file in fList.Except(alreadyProcessedFiles).Except(fileEx)) //Exclude already treated ones and bad files
            {
                try
                {
                    CollectXmlFileInfo(ref xNode, file);

                    alreadyProcessedFiles.Add(file);
                }
                catch
                {
                    fileEx.Add(file);

                    Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("File '" + file.FullName + "' corrupted"));

                    ComputeFileInfo(fList, ref xNode, fileEx, alreadyProcessedFiles, fileExtensions);
                }
            }
        }

        //main function, returns the XML of a entire directory (subdirectories included)
        internal static XElement GetDirectoryXml(String dir, string[] fileExtensions)
        {
            //Create DirectoryInfo
            DirectoryInfo Dir = new DirectoryInfo(dir);

            //Will be used to store the bad files
            List<FileInfo> BadFiles = new List<FileInfo>();

            //Will be used to store the processed files
            List<FileInfo> AlreadyProcessedFiles = new List<FileInfo>();

            //Mandatory to avoid invalid xml
            var info = new XElement("Root");

            //Gather file information
            ComputeFileInfo(Dir.EnumerateFiles().Where(f => fileExtensions.Contains(f.Extension.ToLower())).ToArray(),
                            ref info,
                            BadFiles,
                            AlreadyProcessedFiles,
                            fileExtensions);

            foreach (var subDir in Dir.EnumerateDirectories())
            {
                info.Add(GetDirectoryXml(subDir.FullName, fileExtensions));
            }

            return info;
        }

        #endregion
    }
}