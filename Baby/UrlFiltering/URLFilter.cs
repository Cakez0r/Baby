using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Baby.UrlFiltering
{
    public class URLFilter
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(URLFilter));

        Dictionary<string, Func<Uri, bool>> m_rules = new Dictionary<string, Func<Uri, bool>>();

        /// <summary>
        /// Add a rule that will validate whether a URL is valid
        /// </summary>
        public void AddRule(string ruleName, Func<Uri, bool> rule)
        {
            if (rule != null && !m_rules.ContainsKey(ruleName))
            {
                s_logger.DebugFormat("Adding new URL filtering rule: {0}", ruleName);
                m_rules.Add(ruleName, rule);
            }
        }

        public void RemoveRule(string ruleName)
        {
            s_logger.DebugFormat("Removing URL filtering rule: {0}", ruleName);
            m_rules.Remove(ruleName);
        }

        /// <summary>
        /// Checks a url against all rules in this filter
        /// </summary>
        public bool IsUrlValid(Uri url)
        {
            foreach (KeyValuePair<string, Func<Uri, bool>> rule in m_rules)
            {
                if (!rule.Value(url))
                {
                    s_logger.DebugFormat("URL {0} filtered by rule {1}", url.AbsoluteUri, rule.Key);
                    return false;
                }
            }
            
            return true;
        }
    }
}
