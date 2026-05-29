using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Uplauncher.Classes
{
    public class PackerHelper
    {
        public static void Unpack(string Source, string Destination)
        {
            using (FileStream fsSource = new FileStream(Source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (File.Exists(Destination))
                {
                    File.Delete(Destination);
                }

                using (FileStream fsDest = new FileStream(Destination, FileMode.OpenOrCreate, FileAccess.Write))
                using (GZipStream gzDeCompressed = new GZipStream(fsSource, CompressionMode.Decompress))
                {
                    PackerHelper.CopyStream(gzDeCompressed, fsDest);
                }
            }
        }

        public static string HashFromFile(string File)
        {
            using (FileStream file = new FileStream(File, FileMode.Open, FileAccess.Read, FileShare.Read))
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

        public static int GetFileLength(string File)
        {
            using (FileStream file = new FileStream(File, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long retVal = file.Length;
                return (int)retVal;
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
