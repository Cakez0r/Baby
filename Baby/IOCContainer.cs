using Baby.Crawler;
using Baby.Crawler.PageFetching;
using Baby.UrlFiltering;
using log4net;
using Microsoft.Practices.Unity;
using Baby.Data;

namespace Baby
{
    public static class IOCContainer
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(IOCContainer));

        private static UnityContainer s_container;
        public static UnityContainer Instance
        {
            get { return s_container; }
        }

        static IOCContainer()
        {
            s_logger.Debug("Initialising IOC...");

            s_container = new UnityContainer();
            s_container.RegisterType<IAsyncWebpageProvider, GZipWebClient>();
            s_container.RegisterType<IAsyncEmailAndUrlListProvider, WebpageScraper>();
            s_container.RegisterType<IUrlBlacklist, HashsetBlacklist>();

            foreach (ContainerRegistration reg in s_container.Registrations)
            {
                s_logger.DebugFormat("{0} is mapped to {1}", reg.RegisteredType.Name, reg.MappedToType.Name);
            }
        }
    }
}
