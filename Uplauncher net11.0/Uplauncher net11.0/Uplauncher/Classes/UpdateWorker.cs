using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Uplauncher.Classes
{
    public class UpdateWorker : BaseBackgroundWorker
    {
        public struct UpdateWorkerProgressState
        {
            public UpdateWorkerState State
            {
                get;
                set;
            }

            public int ServerFileCount
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

            public int FilesLeft
            {
                get;
                set;
            }

            public int BytesTotal
            {
                get;
                set;
            }

            public int BytesLoaded
            {
                get;
                set;
            }

            public int DiffFileCount
            {
                get;
                set;
            }

            public UpdateWorker.DoWorkStatictic statistics
            {
                get;
                set;
            }
        }

        public enum UpdateWorkerState
        {
            Starting,
            FetchingVer,
            FetchingVerInfo,
            CheckingDiff,
            FetchingDiff,
            UpdatingDiff,
            FileDownloaded,
            Finished,
            CheckingFiles,
            CheckingFilesError,
            VerinfoFail
        }

        public class DoWorkStatictic
        {
            public int totalLoadFiles;

            public int totalUnprocessFiles;

            public int totalDeletedFiles;

            public List<string> unloadFiles = new List<string>();
        }

        public bool Cancel = false;

        public bool Delayed = false;

        public enum VerInfoResult
        {
            Done,
            Fail,
            Canceled,
            FileFail
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UpdateWorkerState State
        {
            get;
            set;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ServerVersionHash
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

        public UpdateWorker()
        {
            this.Stopwatch = new Stopwatch();
            this.State = UpdateWorkerState.Starting;
            base.DoWork += new DoWorkEventHandler(this.UpdateWorker_DoWork);
        }

        public void Close()
        {
            this.Cancel = true;
        }

        public void Pause()
        {
            this.Delayed = true;
        }

        public void Unpause()
        {
            this.Delayed = false;
        }


        private void UpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Cancel = false;
            base.ReportProgress(0, new UpdateWorkerProgressState
            {
                State = UpdateWorkerState.Starting
            });
            if (this.CompareServerAndLocalVerRec())
            {
                base.ReportProgress(100, new UpdateWorkerProgressState
                {
                    State = UpdateWorkerState.Finished
                });
            }
            else
            {
                VerInfo verInfo;
                VerInfo serverVerInfo;
                VerInfo localVerInfo;
                this.DoVerInfo(out verInfo, out serverVerInfo, out localVerInfo);
                if (!this.Cancel)
                {
                    UpdateWorker.VerInfoResult res = this.TestVerInfo(verInfo);
                    if (res == UpdateWorker.VerInfoResult.Done)
                    {
                        UpdateWorker.DoWorkStatictic stat = this.DoUpdate(verInfo, localVerInfo);
                        this.FinishUp(stat);
                    }
                    else
                    {
                        if (res == UpdateWorker.VerInfoResult.Fail)
                        {
                            base.ReportProgress(0, new UpdateWorkerProgressState
                            {
                                State = UpdateWorkerState.VerinfoFail
                            });
                        }
                        Fetcher.ClearTempVerInfo();
                    }
                }
            }
        }

        private bool CompareServerAndLocalVerRec()
        {
            base.ReportProgress(0, new UpdateWorkerProgressState
            {
                State = UpdateWorkerState.FetchingVer
            });
            bool result;
            try
            {
                this.ServerVersionHash = Fetcher.FetchServerVerRec();
            }
            catch
            {
                result = true;
                return result;
            }
            this.LocalVersionHash = Fetcher.FetchLocalVerRec();
            result = (this.ServerVersionHash == this.LocalVersionHash);
            return result;
        }

        private void DoVerInfo(out VerInfo result, out VerInfo ServerVerInfo, out VerInfo LocalVerInfo)
        {
            base.ReportProgress(0, new UpdateWorkerProgressState
            {
                State = UpdateWorkerState.FetchingVerInfo
            });
            ServerVerInfo = VerInfo.Parse(Fetcher.FetchServerVerInfo());
            LocalVerInfo = VerInfo.Parse(Fetcher.FetchLocalVerInfo());
            result = ServerVerInfo.Difference(LocalVerInfo);
            if (Fetcher.HasPreviousUpdate(this.ServerVersionHash))
            {
                VerInfo PreviousUpdate = VerInfo.Parse(Fetcher.FetchPreviousUpdate(this.ServerVersionHash));
                VerInfo RemoveVerInfo = PreviousUpdate.DifferenceWithoutHash(ServerVerInfo);
                result = result.Difference(PreviousUpdate);
                foreach (KeyValuePair<string, VerInfoEntry> CurrentEntry in RemoveVerInfo.Files)
                {
                    VerInfoEntry removeEnrty = new VerInfoEntry();
                    removeEnrty.FileHash = "0";
                    removeEnrty.FileInfo = CurrentEntry.Value.FileInfo;
                    removeEnrty.FileName = "-" + CurrentEntry.Value.FileName;
                    removeEnrty.FileLength = 0;
                    result.Files.Add(CurrentEntry.Key, removeEnrty);
                }
            }
            base.ReportProgress(0, new UpdateWorkerProgressState
            {
                State = UpdateWorkerState.FetchingDiff,
                DiffFileCount = result.Files.Count
            });
        }

        private UpdateWorker.DoWorkStatictic DoUpdate(VerInfo UpdateList, VerInfo LocalVerInfo)
        {
            UpdateWorker.DoWorkStatictic stat = new UpdateWorker.DoWorkStatictic();
            List<string> verInfoLines = new List<string>();
            Dictionary<string, int> verInfoLinesIndexes = new Dictionary<string, int>();
            int bytesTotal = 0;
            foreach (VerInfoEntry CurrentEntry in UpdateList.Files.Values)
            {
                verInfoLines.Add(string.Concat(new object[]
                {
                    CurrentEntry.FileName,
                    ",",
                    CurrentEntry.FileHash,
                    ",",
                    CurrentEntry.FileLength
                }));
                verInfoLinesIndexes.Add(CurrentEntry.FileName, verInfoLines.Count - 1);
                bytesTotal += CurrentEntry.FileLength;
            }
            int currentFile = 0;
            int bytesLoaded = 0;
            DateTime lastSaveTime = DateTime.Now;
            string localVerInfoName = Configuration.LoadEntry("LocalVerInfo", null);
            UpdateWorker.DoWorkStatictic result;
            foreach (VerInfoEntry CurrentEntry in UpdateList.Files.Values)
            {
                if (this.Cancel)
                {
                    base.ReportProgress(0, new UpdateWorkerProgressState
                    {
                        State = UpdateWorkerState.Finished
                    });
                    result = stat;
                    return result;
                }
                if (this.Delayed)
                {
                    while (this.Delayed)
                    {
                        if (this.Cancel)
                        {
                            base.ReportProgress(0, new UpdateWorkerProgressState
                            {
                                State = UpdateWorkerState.Finished
                            });
                            result = stat;
                            return result;
                        }
                        Thread.Sleep(300);
                    }
                }
                base.ReportProgress(100 * currentFile++ / UpdateList.Files.Count, new UpdateWorkerProgressState
                {
                    State = UpdateWorkerState.UpdatingDiff,
                    CurrentFile = CurrentEntry.FileInfo.Name,
                    FilesLeft = UpdateList.Files.Count + 1 - currentFile,
                    BytesTotal = bytesTotal,
                    BytesLoaded = bytesLoaded
                });
                try
                {
                    if (CurrentEntry.FileName.StartsWith("-"))
                    {
                        string filename = CurrentEntry.FileName.Substring(1);
                        if (File.Exists(filename))
                        {
                            File.Delete(filename);
                            stat.totalDeletedFiles++;
                        }
                        if (verInfoLinesIndexes.ContainsKey(filename))
                        {
                            verInfoLines[verInfoLinesIndexes[filename]] = "";
                        }
                    }
                    else
                    {
                        bool fileOk = false;
                        for (int loadingTry = 0; loadingTry < 3; loadingTry++)
                        {
                            string _File = Fetcher.FetchFileFromServer(CurrentEntry.FileName);
                            if (File.Exists(_File))
                            {
                                string Original = _File.Replace(".pack", "");
                                PackerHelper.Unpack(_File, Original);
                                int currentEntryLoadedBytes = 0;
                                string currentEntryLoadedHash = "";
                                this.WriteProgressEntry(_File, out currentEntryLoadedBytes, out currentEntryLoadedHash);
                                if (!(CurrentEntry.FileHash != currentEntryLoadedHash))
                                {
                                    bytesLoaded += currentEntryLoadedBytes;
                                    File.Delete(_File);
                                    stat.totalLoadFiles++;
                                    if (verInfoLinesIndexes.ContainsKey(CurrentEntry.FileName))
                                    {
                                        verInfoLines[verInfoLinesIndexes[CurrentEntry.FileName]] = string.Concat(new object[]
                                        {
                                            CurrentEntry.FileName,
                                            ",",
                                            CurrentEntry.FileHash,
                                            ",",
                                            CurrentEntry.FileLength
                                        });
                                    }
                                    else
                                    {
                                        verInfoLines.Add(string.Concat(new object[]
                                        {
                                            CurrentEntry.FileName,
                                            ",",
                                            CurrentEntry.FileHash,
                                            ",",
                                            CurrentEntry.FileLength
                                        }));
                                        verInfoLinesIndexes.Add(CurrentEntry.FileName, verInfoLines.Count - 1);
                                    }
                                    fileOk = true;
                                    break;
                                }
                            }
                        }
                        if (!fileOk)
                        {
                            stat.unloadFiles.Add(CurrentEntry.FileName);
                            stat.totalUnprocessFiles++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.PublishExceptionLog(e);
                    stat.totalUnprocessFiles++;
                }
                if ((DateTime.Now - lastSaveTime).TotalSeconds > 3.0)
                {
                    lastSaveTime = DateTime.Now;
                    File.WriteAllLines(localVerInfoName, verInfoLines.ToArray());
                }
            }
            result = stat;
            return result;
        }

        private void FinishUp(UpdateWorker.DoWorkStatictic stat)
        {
            try
            {
                if (Fetcher.HasPreviousUpdate(this.ServerVersionHash))
                {
                    File.Delete(string.Format(".\\{0}", this.ServerVersionHash));
                }

                Fetcher.SwapVerFiles(stat);
                base.ReportProgress(0, new UpdateWorkerProgressState
                {
                    State = UpdateWorkerState.Finished,
                    statistics = stat
                });
            }
            catch (Exception ex)
            {
                this.Cancel = true;
                Log.PublishExceptionLog(ex);
                Fetcher.ClearTempVerInfo();
                base.ReportProgress(0, new UpdateWorkerProgressState
                {
                    State = UpdateWorkerState.VerinfoFail
                });
            }
        }

        private void WriteProgressEntry(string Entry, out int bytesLoaded, out string entryHash)
        {
            StreamWriter Writer = new StreamWriter(string.Format(".\\{0}", this.ServerVersionHash), true);
            string ShortFile = Entry.Replace(string.Format("{0}\\", Application.StartupPath), "").Replace("\\", "/").Replace(".pack", "");
            bytesLoaded = PackerHelper.GetFileLength(ShortFile);
            entryHash = PackerHelper.HashFromFile(ShortFile);
            Writer.Write(string.Format("{0},{1},{2}\r\n", ShortFile, entryHash, bytesLoaded));
            Writer.Close();
        }

        private UpdateWorker.VerInfoResult TestVerInfo(VerInfo UpdateList)
        {
            base.ReportProgress(0, new UpdateWorkerProgressState
            {
                State = UpdateWorkerState.CheckingFiles
            });
            UpdateWorker.VerInfoResult result;
            foreach (VerInfoEntry CurrentEntry in UpdateList.Files.Values)
            {
                if (this.Cancel)
                {
                    result = UpdateWorker.VerInfoResult.Canceled;
                    return result;
                }
                try
                {
                    string filename = CurrentEntry.FileName;
                    if (CurrentEntry.FileName.StartsWith("-"))
                    {
                        filename = CurrentEntry.FileName.Substring(1);
                    }
                    if (File.Exists(filename))
                    {
                        try
                        {
                            Stream fs = File.Open(filename, FileMode.Open, FileAccess.Write);
                            fs.Close();
                        }
                        catch
                        {
                            base.ReportProgress(0, new UpdateWorkerProgressState
                            {
                                State = UpdateWorkerState.CheckingFilesError,
                                CurrentFile = filename
                            });
                            result = UpdateWorker.VerInfoResult.FileFail;
                            return result;
                        }
                    }
                }
                catch
                {
                    result = UpdateWorker.VerInfoResult.Fail;
                    return result;
                }
            }
            result = UpdateWorker.VerInfoResult.Done;
            return result;
        }
    }
}
