using System;
using System.Collections.Generic;
using log4net;

namespace Baby.Data
{
    public class HashsetBlacklist : IUrlBlacklist
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlBlacklist));

        HashSet<string> m_blacklist = new HashSet<string>();

        public string Name
        {
            get;
            set;
        }

        public HashsetBlacklist()
        {
            Name = string.Empty;
        }

        public void AddUrlToBlacklist(Uri url)
        {
            lock (m_blacklist)
            {
                s_logger.DebugFormat("[{0}] Blacklisting URL: {1}", Name, url.AbsoluteUri);
                m_blacklist.Add(url.AbsoluteUri);
            }
        }

        public void RemoveUrlFromBlacklist(Uri url)
        {
            lock (m_blacklist)
            {
                s_logger.DebugFormat("[{0}] Removing URL from blacklist: {1}", Name, url.AbsoluteUri);
                m_blacklist.Remove(url.AbsoluteUri);
            }
        }

        public bool IsUrlBlacklisted(Uri url)
        {
            lock (m_blacklist)
            {
                return m_blacklist.Contains(url.AbsoluteUri);
            }
        }
    }
}
