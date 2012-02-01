using System;
using System.Collections.Generic;

namespace Baby.Crawler.EmailFetching
{
    public interface IAsyncEmailListProvider
    {
        string Source { get; }

        void GetEmailListAsync(Action<IList<EmailAddress>, IAsyncEmailListProvider> completionCallback, Action<Exception, IAsyncEmailListProvider> errorCallback);
    }
}
