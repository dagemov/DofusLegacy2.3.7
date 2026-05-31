using System;
using System.Net;

namespace Uplauncher.Classes
{
    internal class IWebClient
    {
        public static byte[] DownloadBytes(string url)
        {
            byte[] result;
            using (WebClient webClient = new WebClient())
            {
                result = webClient.DownloadData(url);
            }
            return result;
        }
    }
}
