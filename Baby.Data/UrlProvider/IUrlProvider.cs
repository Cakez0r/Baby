using System;
using System.Collections.Generic;

namespace Baby.Data
{
    public interface IUrlProvider : IDisposable
    {
        long UrlCount { get; }

        Uri GetUri();

        IList<Uri> GetUrls(int count);

        void EnqueueUrl(Uri url);
    }
}
