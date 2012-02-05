using System;
using log4net;
using BookSleeve;
using System.Threading.Tasks;
using System.Configuration;

namespace Baby.Data
{
    public class RedisUrlProvider : IUrlProvider
    {
        private const string REDIS_QUEUE_NAME = "UrlProvider";

        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlBlacklist));

        RedisConnection m_redisConnection = new RedisConnection(ConfigurationManager.AppSettings["RedisHost"]);

        public RedisUrlProvider()
        {
            //connect
            s_logger.Info("Connecting to redis for url provider...");
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

        public Uri GetUri()
        {
            Task<string> result = m_redisConnection.Lists.RemoveFirstString(0, REDIS_QUEUE_NAME);
            result.Wait();

            return result.Result != null ? new Uri(result.Result) : null;
        }

        public void EnqueueUrl(Uri url)
        {
            m_redisConnection.Lists.AddLast(0, REDIS_QUEUE_NAME, url.AbsoluteUri);
        }
    }
}
