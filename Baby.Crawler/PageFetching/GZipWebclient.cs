using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Baby.Crawler.PageFetching
{
    public class GZipWebClient : WebClient, IAsyncWebpageProvider
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }

        /// <summary>
        /// Begins asynchronously fetching a webpage
        /// </summary>
        public void GetWebpageAsync(Uri url, Action<string> completionCallback, Action<Exception> errorCallback)
        {
            this.DownloadStringCompleted += (sender, e) =>
                {
                    if (completionCallback != null)
                    {
                        if (e.Error == null)
                        {
                            completionCallback(e.Result);
                        }
                        else
                        {
                            errorCallback(e.Error);
                        }
                    }
                };

            this.DownloadStringAsync(url);
        }
    }
}
