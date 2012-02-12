using System;
using log4net;
using BookSleeve;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;

namespace Baby.Data
{
    public class RedisUrlProvider : IUrlProvider
    {
        private const string REDIS_QUEUE_NAME = "UrlProvider";

        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlProvider));

        private RedisConnection m_redisConnection = new RedisConnection(ConfigurationManager.AppSettings["RedisHost"]);

        public long UrlCount
        {
            get 
            {
                Task<long> result = m_redisConnection.Lists.GetLength(0, REDIS_QUEUE_NAME);
                result.Wait();

                return result.Result; //Truncation shouldn't be a problem here... Should it? :P
            }
        }

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

        public void Dispose()
        {
            m_redisConnection.Dispose();
        }


        public IList<Uri> GetUrls(int count)
        {
            Task<string[]> result = m_redisConnection.Lists.RangeString(0, REDIS_QUEUE_NAME, 0, count, false);
            result.Wait();

            List<Uri> urls = new List<Uri>(result.Result.Length);

            foreach (string url in result.Result)
            {
                urls.Add(new Uri(url));
            }

            return urls;
        }
    }
}
