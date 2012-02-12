using System;
using MySql.Data.MySqlClient;
using System.Configuration;
using log4net;
using System.Data;
using System.Collections.Generic;

namespace Baby.Data
{
    public class MySQLUrlProvider : IUrlProvider
    {
        //Stored procedure names
        private const string GET_PROCEDURE_NAME = "GET_QueuedUrl";
        private const string GET_N_PROCEDURE_NAME = "GET_QueuedUrls";
        private const string QUEUE_PROCEDURE_NAME = "INS_QueuedUrl";
        private const string GET_COUNT_NAME = "GET_QueuedUrlCount";

        private static ILog s_logger = LogManager.GetLogger(typeof(IUrlProvider));

        private MySqlConnection m_connection = new MySqlConnection(ConfigurationManager.AppSettings["MySQLConnectionString"]);

        public MySQLUrlProvider()
        {
            //Connect
            try
            {
                m_connection.Open();
            }
            catch (Exception ex)
            {
                s_logger.ErrorFormat("Exception in MySQL connection: {0}", ex);
            }
        }

        public long UrlCount
        {
            get 
            {
                lock (m_connection)
                {
                    //Execute stored proc to get URL count
                    using (MySqlCommand countCommand = new MySqlCommand())
                    {
                        countCommand.Connection = m_connection;
                        countCommand.CommandType = CommandType.StoredProcedure;
                        countCommand.CommandText = GET_COUNT_NAME;

                        return (long)countCommand.ExecuteScalar();
                    }
                }
            }
        }

        public Uri GetUri()
        {
            lock (m_connection)
            {
                //Execute stored proc to get url
                using (MySqlCommand getCommand = new MySqlCommand())
                {
                    getCommand.Connection = m_connection;
                    getCommand.CommandType = CommandType.StoredProcedure;
                    getCommand.CommandText = GET_PROCEDURE_NAME;

                    Uri url = null;

                    while (url == null)
                    {
                        using (MySqlDataReader result = getCommand.ExecuteReader())
                        {
                            try
                            {
                                result.Read();
                            }
                            catch (Exception ex)
                            {
                                s_logger.WarnFormat("Failed to read a url from MySQL Url Provider. {0}", ex);
                            }

                            try
                            {
                                url = new Uri(result.GetString(0));
                            }
                            catch (Exception ex)
                            {
                                s_logger.WarnFormat("Failed to parse url from MySQL Url Provider. {0}", ex);
                            }
                        }
                    }

                    return url;
                }
            }
        }

        public void EnqueueUrl(Uri url)
        {
            lock (m_connection)
            {
                //Execute stored proc to queue a url
                using (MySqlCommand queueCommand = new MySqlCommand())
                {
                    queueCommand.Connection = m_connection;
                    queueCommand.CommandType = CommandType.StoredProcedure;
                    queueCommand.CommandText = QUEUE_PROCEDURE_NAME;

                    queueCommand.Parameters.AddWithValue("url", url.AbsoluteUri);

                    queueCommand.ExecuteNonQuery();
                }
            }
        }

        public void Dispose()
        {
            m_connection.Dispose();
        }

        public IList<Uri> GetUrls(int count)
        {
            lock (m_connection)
            {
                //Execute stored proc to get bulk urls
                using (MySqlCommand getCommand = new MySqlCommand())
                {
                    getCommand.Connection = m_connection;
                    getCommand.CommandType = CommandType.StoredProcedure;
                    getCommand.CommandText = GET_N_PROCEDURE_NAME;

                    getCommand.Parameters.AddWithValue("count", count);

                    using (MySqlDataReader result = getCommand.ExecuteReader())
                    {
                        List<Uri> urls = new List<Uri>(count);

                        while (result.Read())
                        {
                            try
                            {
                                urls.Add(new Uri(result.GetString(0)));
                            }
                            catch (Exception ex)
                            {
                                s_logger.WarnFormat("Failed to parse url from MySQL Url Provider. {0}", ex);
                            }
                        }

                        return urls;
                    }
                }
            }
        }
    }
}
