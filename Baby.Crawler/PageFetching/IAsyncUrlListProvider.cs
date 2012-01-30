using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Crawler.PageFetching
{
    public interface IAsyncUrlListProvider
    {
        void GetUrlListAsync(Action<IList<Uri>> completionCallback, Action<Exception> errorCallback);
    }
}
