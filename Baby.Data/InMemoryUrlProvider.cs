using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Data
{
    public class InMemoryUrlProvider : IUrlProvider
    {
        Queue<Uri> m_urls;

        public InMemoryUrlProvider(IEnumerable<Uri> initialQueue)
        {
            m_urls = new Queue<Uri>(initialQueue);
        }

        public void EnqueueUrl(Uri url)
        {
            lock (m_urls)
            {
                m_urls.Enqueue(url);
            }
        }

        public Uri GetUri()
        {
            lock (m_urls)
            {
                return m_urls.Count > 0 ? m_urls.Dequeue() : null;
            }
        }
    }
}
