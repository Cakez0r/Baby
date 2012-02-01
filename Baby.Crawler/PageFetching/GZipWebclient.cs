using System;
using System.Net;
using log4net;

namespace Baby.Crawler.PageFetching
{
    public class GZipWebClient : WebClient, IAsyncWebpageProvider
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IAsyncWebpageProvider));

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }

        /// <summary>
        /// Begins asynchronously fetching a webpage
        /// </summary>
        public void GetWebpageAsync(Uri url, Action<string, IAsyncWebpageProvider> completionCallback, Action<Exception, IAsyncWebpageProvider> errorCallback)
        {
            s_logger.DebugFormat("Beginning fetch of {0}", url.AbsoluteUri);

            this.DownloadStringCompleted += (sender, e) =>
                {
                    if (completionCallback != null)
                    {
                        if (e.Error == null)
                        {
                            s_logger.DebugFormat("Completed fetch of {0}", url.AbsoluteUri);
                            completionCallback(e.Result, this);
                        }
                        else
                        {
                            s_logger.WarnFormat("Fetch of {0} failed: {1}", url.AbsoluteUri, e.Error);
                            errorCallback(e.Error, this);
                        }
                    }
                };

            this.DownloadStringAsync(url);
        }
    }
}
