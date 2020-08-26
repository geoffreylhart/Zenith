using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace STRMDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            // 14297 Files
            // 97.51GB
            string downloadFolder = @"C:\Users\Geoffrey Hart\Downloads";
            string rootUrl = "https://e4ftl01.cr.usgs.gov/MEASURES/SRTMGL1.003/2000.02.11/index.html";
            string rootHtml = GetHTML(rootUrl);
            if (!rootHtml.Contains("Page_1")) throw new NotImplementedException();
            int totalFiles = 0;
            double totalGB = 0;
            List<string> fileUrls = new List<string>();
            foreach (Match folderMatch in Regex.Matches(rootHtml, "<a href=\"([^\"]*?)\">Page_[0-9]+</a>", RegexOptions.Singleline))
            {
                string folderUrl = folderMatch.Groups[1].Value;
                string folderHtml = GetHTML(folderUrl);
                if (!folderHtml.Contains("hgt.zip")) throw new NotImplementedException();
                foreach (Match fileMatch in Regex.Matches(folderHtml, "<a href=\"([^\"]*?\\.hgt\\.zip)\">.*?<td>.*?<td>([^<]*?)</td>", RegexOptions.Singleline))
                {
                    string fileUrl = fileMatch.Groups[1].Value;
                    string fileSize = fileMatch.Groups[2].Value;
                    fileUrls.Add(fileUrl);
                    totalFiles++;
                    totalGB += double.Parse(fileSize.Substring(0, fileSize.Length - 1)) / new[] { 1024.0 * 1024.0, 1024.0, 1.0 }["KMG".IndexOf(fileSize.Last())];
                }
            }
            Console.WriteLine(totalFiles + " Files");
            Console.WriteLine($"{totalGB:F2}GB");
            Console.WriteLine("Enter Password: ");
            string pw = IOHelper.ReadPassword();
            int progress = 0;
            foreach (var fileUrl in fileUrls)
            {
                string downloadPath = Path.Combine(downloadFolder, Path.GetFileName(fileUrl));
                progress++;
                if (!File.Exists(downloadPath))
                {
                    using (var client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential("glhart", pw, "https://e4ftl01.cr.usgs.gov");
                        client.UseDefaultCredentials = true;
                        client.Headers.Add("Cookie", "DATA=X0Rx9vcOpVC6eVXo4DELiAAAABc");
                        client.DownloadFile(fileUrl.Replace("http://", "https://"), downloadPath);
                    }
                    Console.Clear();
                    Console.WriteLine($"{progress / (double)totalFiles * 100:F2}% Done");
                }
            }
        }

        private static string GetHTML(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Host = "my.flexmls.com";
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:68.0) Gecko/20100101 Firefox/68.0";
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //request.Headers["Accept-Language"] = "en-US,en;q=0.5";
            //request.Headers["Accept-Encoding"] = "gzip, deflate";
            //request.Connection = "keep-alive";
            //request.Headers["Upgrade-Insecure-Requests"] = "1";
            //request.Headers["Pragma"] = "no-cache";
            //request.Headers["Cache-Control"] = "no-cache";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
                return data;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
