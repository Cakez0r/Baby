﻿using System;
using System.Collections.Generic;
using log4net;

namespace Baby.Data
{
    public class InMemoryUrlProvider : IUrlProvider
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlProvider));

        Queue<Uri> m_urls;

        public long UrlCount
        {
            get { return m_urls.Count; }
        }

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

        public void Dispose()
        {
            m_urls.Clear();
            m_urls = null;
        }


        public IList<Uri> GetUrls(int count)
        {
            lock (m_urls)
            {
                Uri[] urls = new Uri[Math.Min(count, m_urls.Count)];

                for (int i = 0; i < urls.Length; i++)
                {
                    urls[i] = m_urls.Dequeue();
                }

                return urls;
            }
        }
    }
}
