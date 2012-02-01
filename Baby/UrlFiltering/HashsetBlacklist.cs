using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.UrlFiltering
{
    public class HashsetBlacklist : IUrlBlacklist
    {
        HashSet<string> m_blacklist = new HashSet<string>();

        public void AddUrlToBlacklist(Uri url)
        {
            lock (m_blacklist)
            {
                m_blacklist.Add(url.AbsoluteUri);
            }
        }

        public void RemoveUrlFromBlacklist(Uri url)
        {
            lock (m_blacklist)
            {
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
