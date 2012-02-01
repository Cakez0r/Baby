using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using Baby.Crawler;
using Baby.Crawler.PageFetching;
using Baby.Data;
using Baby.UrlFiltering;

namespace Baby
{
    public static class IOCContainer
    {
        private static UnityContainer s_container;
        public static UnityContainer Instance
        {
            get { return s_container; }
        }

        static IOCContainer()
        {
            s_container = new UnityContainer();
            s_container.RegisterType<IAsyncWebpageProvider, GZipWebClient>();
            s_container.RegisterType<IAsyncEmailAndUrlListProvider, WebpageScraper>();
            s_container.RegisterType<IUrlBlacklist, HashsetBlacklist>();
        }
    }
}
