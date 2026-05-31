using System;
using System.IO;


namespace Uplauncher.Classes
{
    public class VerInfoEntry
    {
        public string FileName
        {
            get;
            set;
        }

        public string FileHash
        {
            get;
            set;
        }

        public FileInfo FileInfo
        {
            get;
            set;
        }

        public int FileLength
        {
            get;
            set;
        }
    }
}
