using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Uplauncher.Classes
{
    public class VersionCheckWorker : BaseBackgroundWorker
    {
        public bool Cancel = false;

        public struct VersionCheckWorkerProgressState
        {
            public VersionCheckWorkerState State
            {
                get;
                set;
            }

            public int LocalFileCount
            {
                get;
                set;
            }

            public string CurrentFile
            {
                get;
                set;
            }
        }

        public enum VersionCheckWorkerState
        {
            Starting,
            GenerateVerInfo,
            Finished
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public VersionCheckWorkerState State
        {
            get;
            set;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LocalVersionHash
        {
            get;
            set;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StreamWriter ProgressStream
        {
            get;
            set;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Stopwatch Stopwatch
        {
            get;
            set;
        }

        public VersionCheckWorker()
        {
            this.Stopwatch = new Stopwatch();
            this.State = VersionCheckWorkerState.Starting;
            base.DoWork += new DoWorkEventHandler(this.VersionCheckWorker_DoWork);
        }

        public void Close()
        {
            this.Cancel = true;
        }

        private void VersionCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            base.ReportProgress(0, new VersionCheckWorkerProgressState
            {
                State = VersionCheckWorkerState.Starting
            });
            string localVerInfoName = Configuration.LoadEntry("LocalVerInfo", null);
            string localVerName = Configuration.LoadEntry("LocalVer", null);
            VerInfo verInfo = VerInfo.Parse(Fetcher.FetchServerVerInfo());
            if (verInfo.Files.Count > 0)
            {
                this.RelalculateHashInVerInfo(ref verInfo);
            }
            else
            {
                verInfo = VerInfo.Parse(Fetcher.FetchLocalVerInfo());
                if (verInfo.Files.Count > 0)
                {
                    this.RelalculateHashInVerInfo(ref verInfo);
                }
                else
                {
                    verInfo.Files = new Dictionary<string, VerInfoEntry>();
                    int count = Directory.GetFiles(Environment.CurrentDirectory, "*.*", SearchOption.AllDirectories).Length;
                    File.WriteAllText(localVerInfoName, "", Encoding.UTF8);
                    this.GenerateHashForFolder(ref verInfo, Environment.CurrentDirectory, Environment.CurrentDirectory, 0, count);
                }
            }
            File.WriteAllText(localVerName, this.GetMD5HashFromFile(localVerInfoName));
            base.ReportProgress(100, new VersionCheckWorkerProgressState
            {
                State = VersionCheckWorkerState.Finished
            });
        }

        private string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private string GetMD5HashFromFile(string fileName)
        {
            using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MD5 md5 = MD5.Create())
            {
                byte[] retVal = md5.ComputeHash(file);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private int RelalculateHashInVerInfo(ref VerInfo verInfo)
        {
            VerInfo newverInfo = new VerInfo();
            newverInfo.Files = new Dictionary<string, VerInfoEntry>();
            string CurrentPath = Environment.CurrentDirectory;
            int count = verInfo.Files.Count;
            int startcount = 0;
            string localVerInfoName = Configuration.LoadEntry("LocalVerInfo", null);
            StringBuilder verinfoNextContent = new StringBuilder();
            File.WriteAllText(localVerInfoName, "", Encoding.UTF8);
            DateTime lastSaveTime = DateTime.Now;
            DateTime lastReportTime = DateTime.Now;
            foreach (VerInfoEntry entry in verInfo.Files.Values)
            {
                string filename = Path.Combine(CurrentPath, entry.FileName);
                if ((DateTime.Now - lastReportTime).TotalSeconds > 0.3)
                {
                    lastReportTime = DateTime.Now;
                    base.ReportProgress((int)(100L * (long)startcount / (long)count), new VersionCheckWorkerProgressState
                    {
                        State = VersionCheckWorkerState.GenerateVerInfo,
                        CurrentFile = entry.FileName
                    });
                }
                try
                {
                    if (File.Exists(filename))
                    {
                        FileInfo f = new FileInfo(filename);
                        VerInfoEntry verEntry = new VerInfoEntry();
                        verEntry.FileName = entry.FileName;
                        verEntry.FileInfo = f;
                        verEntry.FileLength = (int)f.Length;
                        verEntry.FileHash = this.GetMD5HashFromFile(filename);
                        newverInfo.Files.Add(entry.FileName, verEntry);
                        verinfoNextContent.AppendLine(string.Format("{0},{1},{2}", verEntry.FileName, verEntry.FileHash, verEntry.FileLength));
                        if ((DateTime.Now - lastSaveTime).TotalSeconds > 3.0)
                        {
                            lastSaveTime = DateTime.Now;
                            File.AppendAllText(localVerInfoName, verinfoNextContent.ToString(), Encoding.UTF8);
                            verinfoNextContent = new StringBuilder();
                        }
                    }
                }
                catch
                {
                }
                startcount++;
            }
            File.AppendAllText(localVerInfoName, verinfoNextContent.ToString());
            verInfo = newverInfo;
            return startcount;
        }

        private int GenerateHashForFolder(ref VerInfo verInfo, string dir, string begindir, int startcount, int totalcounts)
        {
            string localVerInfoName = Configuration.LoadEntry("LocalVerInfo", null);
            StringBuilder verinfoNextContent = new StringBuilder();
            DateTime lastSaveTime = DateTime.Now;
            DirectoryInfo dirinfo = new DirectoryInfo(dir);
            DirectoryInfo[] directories = dirinfo.GetDirectories();
            for (int i = 0; i < directories.Length; i++)
            {
                DirectoryInfo g = directories[i];
                startcount = this.GenerateHashForFolder(ref verInfo, g.FullName, begindir, startcount, totalcounts);
            }
            DateTime lastReportTime = DateTime.Now;
            FileInfo[] files = dirinfo.GetFiles("*.*");
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo f = files[i];
                string relativepath = this.GetRelativePath(f.FullName, begindir);
                relativepath = relativepath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if ((DateTime.Now - lastReportTime).TotalSeconds > 0.3)
                {
                    lastReportTime = DateTime.Now;
                    base.ReportProgress((int)(100L * (long)startcount / (long)totalcounts), new VersionCheckWorkerProgressState
                    {
                        State = VersionCheckWorkerState.GenerateVerInfo,
                        CurrentFile = relativepath
                    });
                }
                VerInfoEntry verEntry = new VerInfoEntry();
                verEntry.FileName = relativepath;
                verEntry.FileInfo = f;
                verEntry.FileLength = (int)f.Length;
                verEntry.FileHash = this.GetMD5HashFromFile(verEntry.FileName);
                verinfoNextContent.AppendLine(string.Format("{0},{1},{2}", verEntry.FileName, verEntry.FileHash, verEntry.FileLength));
                if ((DateTime.Now - lastSaveTime).TotalSeconds > 3.0)
                {
                    lastSaveTime = DateTime.Now;
                    File.AppendAllText(localVerInfoName, verinfoNextContent.ToString(), Encoding.UTF8);
                    verinfoNextContent = new StringBuilder();
                }
                startcount++;
            }
            File.AppendAllText(localVerInfoName, verinfoNextContent.ToString());
            return startcount;
        }
    }
}
