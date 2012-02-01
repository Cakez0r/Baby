using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.UrlFiltering
{
    public class URLFilter
    {
        List<Func<Uri, bool>> m_rules = new List<Func<Uri,bool>>();

        /// <summary>
        /// Add a rule that will validate whether a URL is valid
        /// </summary>
        public void AddRule(Func<Uri, bool> rule)
        {
            if (rule != null)
            {
                m_rules.Add(rule);
            }
        }

        /// <summary>
        /// Checks a url against all rules in this filter
        /// </summary>
        public bool IsUrlValid(Uri url)
        {
            return m_rules.All(rule => rule(url));
        }
    }
}
