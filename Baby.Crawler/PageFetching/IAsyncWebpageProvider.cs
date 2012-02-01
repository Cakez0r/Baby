using System;

namespace Baby.Crawler.PageFetching
{
    public interface IAsyncWebpageProvider
    {
        void GetWebpageAsync(Uri url, Action<string, IAsyncWebpageProvider> completionCallback, Action<Exception, IAsyncWebpageProvider> errorCallback);
    }
}
