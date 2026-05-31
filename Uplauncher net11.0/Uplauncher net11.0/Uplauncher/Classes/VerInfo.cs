using System;
using System.Collections.Generic;
using System.IO;


namespace Uplauncher.Classes
{
    public class VerInfo
    {
        public Dictionary<string, VerInfoEntry> Files
        {
            get;
            set;
        }

        public VerInfo Difference(VerInfo Source)
        {
            List<string> KeysToRemove = new List<string>();
            foreach (KeyValuePair<string, VerInfoEntry> CurrentEntry in Source.Files)
            {
                if (this.Files.ContainsKey(CurrentEntry.Key) && CurrentEntry.Value.FileHash == this.Files[CurrentEntry.Key].FileHash)
                {
                    KeysToRemove.Add(CurrentEntry.Key);
                }
            }
            foreach (string CurrentKey in KeysToRemove)
            {
                this.Files.Remove(CurrentKey);
            }
            return this;
        }

        public VerInfo DifferenceWithoutHash(VerInfo Source)
        {
            List<string> KeysToRemove = new List<string>();
            foreach (KeyValuePair<string, VerInfoEntry> CurrentEntry in Source.Files)
            {
                if (this.Files.ContainsKey(CurrentEntry.Key))
                {
                    KeysToRemove.Add(CurrentEntry.Key);
                }
            }
            foreach (string CurrentKey in KeysToRemove)
            {
                this.Files.Remove(CurrentKey);
            }
            return this;
        }

        public static VerInfo Parse(string[] Lines)
        {
            VerInfo Response = new VerInfo();
            Response.Files = new Dictionary<string, VerInfoEntry>();
            int CurrentLineIndex = 0;
            try
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    string CurrentLine = Lines[i];
                    string[] CurrentParts = CurrentLine.Split(new char[]
                    {
                        ','
                    });
                    if (CurrentParts.Length == 3)
                    {
                        VerInfoEntry CurrentEntry = new VerInfoEntry();
                        CurrentEntry.FileName = CurrentParts[0];
                        if (CurrentEntry.FileName.StartsWith("-"))
                        {
                            CurrentEntry.FileInfo = new FileInfo(CurrentParts[0].Substring(1));
                        }
                        else
                        {
                            CurrentEntry.FileInfo = new FileInfo(CurrentParts[0]);
                        }
                        CurrentEntry.FileLength = int.Parse(CurrentParts[2]);
                        CurrentEntry.FileHash = CurrentParts[1];
                        Response.Files.Add(CurrentParts[0], CurrentEntry);
                        CurrentLineIndex++;
                    }
                }
            }
            catch
            {
            }
            return Response;
        }
    }
}
