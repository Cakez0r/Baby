using System;
using System.Threading.Tasks;
using BookSleeve;
using log4net;
using System.Configuration;

namespace Baby.Data
{
    public class RedisBlacklist : IUrlBlacklist
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlBlacklist));

        RedisConnection m_redisConnection = new RedisConnection(ConfigurationManager.AppSettings["RedisHost"]);

        public string Name
        {
            get;
            set;
        }

        public RedisBlacklist()
        {
            //Give us a random name for now
            Name = "Blacklist_" + Guid.NewGuid().ToString();

            //connect
            s_logger.Info("Connecting to redis for blacklist...");
            m_redisConnection.Error += new EventHandler<ErrorEventArgs>(RedisConnection_Error);
            Task result = m_redisConnection.Open();
            result.Wait();
        }

        private void RedisConnection_Error(object sender, ErrorEventArgs e)
        {
            if (e.IsFatal)
            {
                s_logger.ErrorFormat("Fatal redis error: {0} - {1}", e.Cause, e.Exception);
            }
            else
            {
                s_logger.WarnFormat("Redis error: {0} - {1}", e.Cause, e.Exception);
            }
        }

        public bool IsUrlBlacklisted(Uri url)
        {
            if (m_redisConnection.State == RedisConnectionBase.ConnectionState.Open)
            {
                Task<bool> result = m_redisConnection.Sets.Contains(0, Name, url.AbsoluteUri);
                result.Wait();
                return result.Result;
            }
            else
            {
                s_logger.WarnFormat("Tried to check if url {0} is blacklistedon {1} when redis connection wasn't open", url.AbsolutePath, Name);
            }

            return false;
        }

        public void AddUrlToBlacklist(Uri url)
        {
            if (m_redisConnection.State == RedisConnectionBase.ConnectionState.Open)
            {
                Task<bool> result = m_redisConnection.Sets.Add(0, Name, url.AbsoluteUri);
                result.Wait();
            }
            else
            {
                s_logger.WarnFormat("Tried to blacklist a {0} on {1} when redis connection wasn't open", url.AbsolutePath, Name);
            }
        }
    }
}