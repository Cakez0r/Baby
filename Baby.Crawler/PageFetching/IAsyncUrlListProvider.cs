using System;
using System.Collections.Generic;

namespace Baby.Crawler.PageFetching
{
    public interface IAsyncUrlListProvider
    {
        string Source { get; }

        void GetUrlListAsync(Action<IList<Uri>, IAsyncUrlListProvider> completionCallback, Action<Exception, IAsyncUrlListProvider> errorCallback);
    }
}
