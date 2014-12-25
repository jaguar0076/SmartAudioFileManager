using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartAudioFileManager
{
    internal static class Utils
    {
        #region Variables
        //Should be stored in a config file
        private static string[] ExcludedString = { "\\", "/", "?", ":", "*", "\"", ">", "<", "|" };

        #endregion

        #region Check property & method name

        private static bool HasProperty(this object o, string propertyName)
        {
            return o.GetType().GetProperty(propertyName) != null;
        }

        private static bool HasMethod(this object o, string methodName)
        {
            return o.GetType().GetMethod(methodName) != null;
        }

        #endregion

        #region Set property & method value

        private static void SetPropertyValue(this object o, string propertyName, bool val)
        {
            o.GetType().GetProperty(propertyName).SetValue(o, val, null);
        }

        private static void SetPropertyValue(this object o, string propertyName, string val)
        {
            o.GetType().GetProperty(propertyName).SetValue(o, val, null);
        }

        private static void SetPropertyValue(this object o, string propertyName, uint val)
        {
            o.GetType().GetProperty(propertyName).SetValue(o, val, null);
        }

        private static void SetMethodValue(this object o, string methodName, string val)
        {
            o.GetType().GetMethod(methodName).Invoke(o, new object[] { val });
        }

        #endregion

        #region Get property value

        private static string GetPropertyValue(this object o, string propertyName)
        {
            string val = "";

            val = (string)o.GetType().GetProperty(propertyName).GetValue(o, null);

            return val;
        }

        #endregion

        #region Check/Set property & method value

        internal static void CheckSetPropertyValue(this object o, string propertyName, bool val)
        {
            if (HasProperty(o, propertyName))
            {
                SetPropertyValue(o, propertyName, val);
            }
            else
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("Error in If statements")); }
        }

        internal static void CheckSetPropertyValue(this object o, string propertyName, string val)
        {
            if (HasProperty(o, propertyName))
            {
                SetPropertyValue(o, propertyName, val);
            }
            else
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("Error in If statements")); }
        }

        internal static void CheckSetPropertyValue(this object o, string propertyName, uint val)
        {
            if (HasProperty(o, propertyName))
            {
                SetPropertyValue(o, propertyName, val);
            }
            else
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("Error in If statements")); }
        }

        internal static string CheckGetPropertyValue(this object o, string propertyName)
        {
            string val = "";

            if (HasProperty(o, propertyName))
            {
                val = GetPropertyValue(o, propertyName);
            }
            else
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("Error in If statements")); }

            return val;
        }

        internal static void CheckSetMethodValue(this object o, string methodName, string val)
        {
            if (HasMethod(o, methodName))
            {
                SetMethodValue(o, methodName, val);
            }
            else
            { Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("Error in If statements")); }
        }

        #endregion

        #region String Formatting utilities

        internal static string CleanString(string txt)
        {
            StringBuilder sb = new StringBuilder(txt);

            return sb.Replace("\0", string.Empty).ToString();
        }

        internal static string NameCleanup(string fileName)
        {//It's frickin faster than Regex, tested with LINQPad 10000000 times, Regex about 11 secondes, between 0.4 and 0.8 secondes with this

            StringBuilder sb = new StringBuilder(fileName);

            foreach (var ExString in ExcludedString)
            {
                sb.Replace(ExString, string.Empty);
            }

            //return Regex.Replace(FileName, @"[\/?:*""><|]+", "-", RegexOptions.Compiled);

            return sb.ToString();
        }

        internal static string FormatStringBasedOnRegex(string fileName, string stringMatch, char stringFormat)
        {
            return Regex.Match(fileName, stringMatch).Value.PadLeft(2, stringFormat);
        }

        #endregion

        #region Logging

        internal static void SaveLogFile(object method, Exception exception)
        {
            string location = Directory.GetCurrentDirectory() + "\\" + "error_log.txt";

            try
            {
                //Opens a new file stream which allows asynchronous reading and writing
                using (StreamWriter sw = new StreamWriter(new FileStream(location, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    //Writes the method name with the exception and writes the exception underneath
                    sw.WriteLine(String.Format("{0} ({1}) - Method: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), method.ToString()));
                    sw.WriteLine(exception.ToString());
                    sw.Write(Environment.NewLine);
                }
            }
            catch (IOException)
            {
                if (!File.Exists(location))
                {
                    File.Create(location);

                    SaveLogFile(method, exception);
                }
            }
            //Utils.SaveLogFile(MethodBase.GetCurrentMethod(), new Exception("MusicBrainzTrackId: " + filetag.Tag.AmazonId));
        }

        #endregion

        #region MD5Hash
        //Be aware of MD5 collision
        internal static string ComputeMD5Hash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream));
                }
            }
        }

        #endregion

        #region Compute Dynamic level

        internal static void ComputeLevel<T>(string curInfo, string path, int curPos, List<T> fileList, params string[] args)
        {
            if (curPos != 0)
            {
                path += "\\" + NameCleanup(curInfo);

                Directory.CreateDirectory(Directory.GetCurrentDirectory() + path);
            }

            curPos++;

            foreach (var CurFileInfo in fileList.GroupBy(i => GetPropertyValue(i, args[curPos - 1]))
                                                        .OrderBy(g => g.Key)
                                                        .Select(g => g.Key))
            {
                if (curPos < args.Length)
                {
                    ComputeLevel<T>(CurFileInfo, path, curPos, fileList.Where(i => GetPropertyValue(i, args[curPos - 1]) == CurFileInfo).ToList(), args);
                }
                else
                {
                    path += "\\" + NameCleanup(CurFileInfo);

                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + path);

                    foreach (var file in fileList.Where(i => GetPropertyValue(i, args[curPos - 1]) == CurFileInfo).ToList())
                    {
                        DbUtils.ExecuteNonQuery("INSERT INTO File(path, title, extension, track, album, year, artists, genres, hash) values (?,?,?,?,?,?,?,?,?)",
                                                CheckGetPropertyValue(file, "FilePath"),
                                                CheckGetPropertyValue(file, "MediaTitle"),
                                                CheckGetPropertyValue(file, "MediaExtension"),
                                                CheckGetPropertyValue(file, "MediaTrack"),
                                                CheckGetPropertyValue(file, "MediaAlbum"),
                                                CheckGetPropertyValue(file, "MediaYear"),
                                                CheckGetPropertyValue(file, "MediaArtists"),
                                                CheckGetPropertyValue(file, "MediaGenres"),
                                                CheckGetPropertyValue(file, "MD5Hash"));

                        string NewFile = (Directory.GetCurrentDirectory() + path) + "\\" +
                                             CheckGetPropertyValue(file, "MediaTrack") +
                                             " - " +
                                             Utils.NameCleanup(CheckGetPropertyValue(file, "MediaTitle")) +
                                             CheckGetPropertyValue(file, "MediaExtension");

                        System.IO.File.Copy(CheckGetPropertyValue(file, "FilePath"), NewFile, true);

                        FileInfo fileInfo = new FileInfo(NewFile);

                        fileInfo.IsReadOnly = false;
                    }
                }
            }
        }

        #endregion
    }
}