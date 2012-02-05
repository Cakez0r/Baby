using System;

namespace Baby.Data
{
    public interface IUrlProvider
    {
        Uri GetUri();

        void EnqueueUrl(Uri url);
    }
}
