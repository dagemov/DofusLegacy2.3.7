using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Uplauncher.Classes
{
    public class Fetcher
    {
        public static string FileBaseUrl = "";

        public static string FetchServerVerRec()
        {
            string LocalPath = Configuration.LoadEntry("LocalTempVer", ".\\data/Launcher/ver.rec.temp");
            bool Packed = Configuration.LoadBooleanEntry("PackedVer", true);
            string LocalPathPacked = string.Format("{0}.pack", LocalPath);
            string uri = Configuration.LoadEntry("VerUri", null);
            byte[] data = IWebClient.DownloadBytes(uri);
            File.WriteAllBytes(Packed ? LocalPathPacked : LocalPath, data);
            if (Packed)
            {
                if (File.Exists(LocalPathPacked))
                {
                    PackerHelper.Unpack(LocalPathPacked, LocalPath);
                    try
                    {
                        if (File.Exists(LocalPathPacked))
                        {
                            File.Delete(LocalPathPacked);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return File.ReadAllText(LocalPath);
        }

        public static string FetchLocalVerRec()
        {
            string entryName = Configuration.LoadEntry("LocalVer", null);
            if (!File.Exists(entryName))
            {
                File.Create(entryName).Close();
            }
            string result;
            if (File.Exists(entryName))
            {
                result = File.ReadAllText(entryName);
            }
            else
            {
                result = "";
            }
            return result;
        }

        public static string[] FetchServerVerInfo()
        {
            string LocalPath = Configuration.LoadEntry("LocalTempVerInfo", ".\\data/Launcher/VerInfo.rec.temp");
            bool Packed = Configuration.LoadBooleanEntry("PackedVerInfo", true);
            string LocalPathPacked = string.Format("{0}.pack", LocalPath);
            string uri = Configuration.LoadEntry("VerInfoUri", null);
            byte[] data = IWebClient.DownloadBytes(uri);
            File.WriteAllBytes(Packed ? LocalPathPacked : LocalPath, data);
            if (Packed)
            {
                if (File.Exists(LocalPathPacked))
                {
                    PackerHelper.Unpack(LocalPathPacked, LocalPath);
                    try
                    {
                        if (File.Exists(LocalPathPacked))
                        {
                            File.Delete(LocalPathPacked);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return File.ReadAllLines(LocalPath);
        }

        public static string[] FetchLocalVerInfo()
        {
            if (!File.Exists(Configuration.LoadEntry("LocalVerInfo", null)))
            {
                File.Create(Configuration.LoadEntry("LocalVerInfo", null)).Close();
            }
            string[] result;
            if (File.Exists(Configuration.LoadEntry("LocalVerInfo", null)))
            {
                result = File.ReadAllLines(Configuration.LoadEntry("LocalVerInfo", null));
            }
            else
            {
                result = new string[0];
            }
            return result;
        }

        public static string FetchFileFromServer(string Path)
        {
            if (Fetcher.FileBaseUrl == "")
            {
                Fetcher.FileBaseUrl = Configuration.LoadEntry("FileBaseUri", null);
            }
            string FileUri = string.Format("{0}{1}", Fetcher.FileBaseUrl, Path);
            string FileLocalPath = string.Format(".\\{0}.pack", Path);
            FileInfo LocalFileInfo = new FileInfo(FileLocalPath);
            if (!Directory.Exists(LocalFileInfo.DirectoryName))
            {
                LocalFileInfo.Directory.Create();
            }
            string result;
            try
            {
                byte[] data = IWebClient.DownloadBytes(FileUri);
                File.WriteAllBytes(FileLocalPath, data);
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    Log.PublishLog(string.Format("Failed to download: '{0}'! Could not connect to file server...", LocalFileInfo.Name));
                    result = LocalFileInfo.Name;
                    return result;
                }
                HttpWebResponse response = (HttpWebResponse)e.Response;
                Log.PublishLog(string.Format("Failed to download: '{0}'! [Code: { 1}]", LocalFileInfo.Name, (int)response.StatusCode));
            }
            catch (Exception e2)
            {
                Log.PublishExceptionWarning(e2, true);
            }
            result = LocalFileInfo.FullName;
            return result;
        }

        public static void SwapVerFiles(object statobj = null)
        {
            if (File.Exists(Configuration.LoadEntry("LocalVerInfo", null)))
            {
                File.Delete(Configuration.LoadEntry("LocalVerInfo", null));
            }
            if (File.Exists(Configuration.LoadEntry("LocalVer", null)))
            {
                File.Delete(Configuration.LoadEntry("LocalVer", null));
            }
            File.Copy(Configuration.LoadEntry("LocalTempVerInfo", null), Configuration.LoadEntry("LocalVerInfo", null));
            File.Copy(Configuration.LoadEntry("LocalTempVer", null), Configuration.LoadEntry("LocalVer", null));
            if (statobj != null)
            {
                try
                {
                    UpdateWorker.DoWorkStatictic stat = (UpdateWorker.DoWorkStatictic)statobj;
                    if (stat.unloadFiles.Count > 0)
                    {
                        VerInfo ver = VerInfo.Parse(Fetcher.FetchLocalVerInfo());
                        foreach (string file in stat.unloadFiles)
                        {
                            if (ver.Files.ContainsKey(file))
                            {
                                ver.Files[file].FileHash = "1";
                            }
                        }
                        List<string> strings = new List<string>();
                        strings.Add("//\n");
                        foreach (VerInfoEntry entry in ver.Files.Values)
                        {
                            strings.Add(string.Concat(new object[]
                            {
                                entry.FileName,
                                ",",
                                entry.FileHash,
                                ",",
                                entry.FileLength,
                                "\n"
                            }));
                        }
                        File.WriteAllLines(Configuration.LoadEntry("LocalVerInfo", null), strings.ToArray());
                        File.WriteAllText(Configuration.LoadEntry("LocalVer", null), "1");
                    }
                }
                catch
                {
                }
            }
            Fetcher.ClearTempVerInfo();
        }

        public static void ClearTempVerInfo()
        {
            if (File.Exists(Configuration.LoadEntry("LocalTempVer", null)))
            {
                File.Delete(Configuration.LoadEntry("LocalTempVer", null));
            }
            if (File.Exists(Configuration.LoadEntry("LocalTempVerInfo", null)))
            {
                File.Delete(Configuration.LoadEntry("LocalTempVerInfo", null));
            }
        }

        public static bool HasPreviousUpdate(string UpdateHash)
        {
            return File.Exists(string.Format(".\\{0}", UpdateHash));
        }

        public static string[] FetchPreviousUpdate(string Hash)
        {
            return File.ReadAllLines(string.Format(".\\{0}", Hash));
        }
    }
}
