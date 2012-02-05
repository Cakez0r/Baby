using System;
using System.Collections.Generic;
using log4net;

namespace Baby.Data
{
    public class InMemoryUrlProvider : IUrlProvider
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlProvider));

        Queue<Uri> m_urls;

        public InMemoryUrlProvider()
        {
            m_urls = new Queue<Uri>();
        }

        public void EnqueueUrl(Uri url)
        {
            lock (m_urls)
            {
                s_logger.DebugFormat("Enqueuing URL {0}", url.AbsoluteUri);
                m_urls.Enqueue(url);
            }
        }

        public Uri GetUri()
        {
            Uri url = null;

            lock (m_urls)
            {
                if (m_urls.Count > 0)
                {
                    url = m_urls.Dequeue();
                }
            }

            s_logger.DebugFormat("Returning URL {0}", url != null ? url.AbsoluteUri : "NULL");

            return url;
        }
    }
}
