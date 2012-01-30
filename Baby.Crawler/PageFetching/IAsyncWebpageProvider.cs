using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Crawler.PageFetching
{
    public interface IAsyncWebpageProvider
    {
        void GetWebpageAsync(Uri url, Action<string> completionCallback, Action<Exception> errorCallback);
    }
}
