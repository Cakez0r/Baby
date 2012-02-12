using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baby.Data
{
    public class CachedUrlProvider<SlowProvider, CacheProvider> : IUrlProvider where SlowProvider : IUrlProvider, new() where CacheProvider : IUrlProvider, new ()
    {
        //A lock to use so that only one fill is done at a time
        private object m_fillLock = new object();

        //The count of URLs to bulk fill from the db
        private const int FILL_LIMIT = 250;

        private CacheProvider m_cache = new CacheProvider();
        private SlowProvider m_slow = new SlowProvider();

        public long UrlCount
        {
            get { return m_cache.UrlCount; }
        }

        public Uri GetUri()
        {
            //Try to get a URL from the cache first
            Uri url = m_cache.GetUri();

            if (url == null)
            {
                //If the cache is empty, try to fill it from the slow provider
                if (Monitor.TryEnter(m_fillLock))
                {
                    //If we have been chosen to fill the cache...
                    if (m_cache.UrlCount == 0)
                    {
                        //Pull URLs in bulk from the slow provider
                        IList<Uri> urls = m_slow.GetUrls(FILL_LIMIT);

                        foreach (Uri u in urls)
                        {
                            //Fill the cache with some URLs
                            m_cache.EnqueueUrl(u);
                        }
                    }

                    //Release the fill lock
                    Monitor.Exit(m_fillLock);
                }
            }

            //Try again to pull a url from the cache
            return url ?? m_cache.GetUri();
        }

        public void EnqueueUrl(Uri url)
        {
            //Write the URL into the cache
            m_cache.EnqueueUrl(url);

            //Async write the url into the slow provider
            Task slowWrite = new Task(() => m_slow.EnqueueUrl(url));
            slowWrite.ContinueWith((t) =>
            {
                if (t.Exception != null)
                {
                    throw t.Exception;
                }
            });

            slowWrite.Start();
        }

        public void Dispose()
        {
            m_slow.Dispose();
            m_cache.Dispose();
        }

        public IList<Uri> GetUrls(int count)
        {
            List<Uri> urls = new List<Uri>(count);

            Uri url = null;
            while ((url = GetUri()) != null || urls.Count >= count)
            {
                urls.Add(url);
            }

            return urls;
        }
    }
}
