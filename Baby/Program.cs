using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Baby.Crawler;
using Baby.Crawler.EmailFetching;
using Baby.Crawler.PageFetching;
using Baby.Data;
using Baby.UrlFiltering;
using log4net;
using Microsoft.Practices.Unity;
using System.Net;

namespace Baby
{
    /// <summary>
    /// Test code until all the components are tested.
    /// Sorry 4chan... totally using you as a testing ground.
    /// </summary>
    class Program
    {
        static InMemoryUrlProvider urlProvider = new InMemoryUrlProvider(new Uri[] { new Uri("http://www.4chan.org") });

        static URLFilter s_urlFilter;
        static IUrlBlacklist s_visitedUrls;

        const int SCRAPER_LIMIT = 50;
        static int s_scraperCount = 0;

        private static ILog s_logger = LogManager.GetLogger(typeof(Program));
        private static ILog s_emailLogger = LogManager.GetLogger("emails");
        private static ILog s_urlLogger = LogManager.GetLogger("urls");

        static HashSet<string> s_emails = new HashSet<string>();

        static void InitializeLogging()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }

        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = SCRAPER_LIMIT;

            InitializeLogging();
            s_logger.Info("Baby is starting!");

            s_logger.DebugFormat("Scraper limit set to {0}", SCRAPER_LIMIT);

            s_logger.Debug("Registering URL provider...");
            IOCContainer.Instance.RegisterInstance<IUrlProvider>(urlProvider);

            s_logger.Debug("Resolving url blacklist...");
            s_visitedUrls = IOCContainer.Instance.Resolve<IUrlBlacklist>();
            s_visitedUrls.Name = "VisitedUrls";

            s_urlFilter = MakeUrlFilter(s_visitedUrls);

            while (true)
            {
                if (s_scraperCount < SCRAPER_LIMIT)
                {
                    s_logger.DebugFormat("Scraper count is at [{0} / {1}]. Spawning a new scraper...", s_scraperCount, SCRAPER_LIMIT);
                    SpawnScraper();
                }

                Thread.Sleep(50);
            }
        }

        static URLFilter MakeUrlFilter(IUrlBlacklist visitedUrlBlacklist)
        {
            s_logger.Debug("Setting up URL filter...");

            URLFilter filter = new URLFilter();

            //Probably a better way to do this with Accept header...
            filter.AddRule("Reject .exe", StandardUrlFilterRules.MakeRejectExtensionRule("exe"));
            filter.AddRule("Reject .css", StandardUrlFilterRules.MakeRejectExtensionRule("css"));
            filter.AddRule("Reject .png", StandardUrlFilterRules.MakeRejectExtensionRule("png"));
            filter.AddRule("Reject .gif", StandardUrlFilterRules.MakeRejectExtensionRule("gif"));
            filter.AddRule("Reject .jpg", StandardUrlFilterRules.MakeRejectExtensionRule("jpg"));
            filter.AddRule("Reject .zip", StandardUrlFilterRules.MakeRejectExtensionRule("zip"));
            filter.AddRule("Reject .7z", StandardUrlFilterRules.MakeRejectExtensionRule("7z"));
            filter.AddRule("Reject .rar", StandardUrlFilterRules.MakeRejectExtensionRule("rar"));
            filter.AddRule("Reject .gz", StandardUrlFilterRules.MakeRejectExtensionRule("gz"));
            filter.AddRule("Reject .avi", StandardUrlFilterRules.MakeRejectExtensionRule("avi"));
            filter.AddRule("Reject .mpg", StandardUrlFilterRules.MakeRejectExtensionRule("mpg"));
            filter.AddRule("Reject .mp3", StandardUrlFilterRules.MakeRejectExtensionRule("mp3"));
            filter.AddRule("Reject .pdf", StandardUrlFilterRules.MakeRejectExtensionRule("pdf"));
            filter.AddRule("Reject .dmg", StandardUrlFilterRules.MakeRejectExtensionRule("dmg"));
            filter.AddRule("Reject .iso", StandardUrlFilterRules.MakeRejectExtensionRule("iso"));
            filter.AddRule("Reject .ico", StandardUrlFilterRules.MakeRejectExtensionRule("ico"));
            filter.AddRule("Reject .rss", StandardUrlFilterRules.MakeRejectExtensionRule("rss"));

            filter.AddRule("Reject hash urls", StandardUrlFilterRules.RejectHashUrls);

            filter.AddRule("Reject JS urls", StandardUrlFilterRules.RejectJavascriptUrl);

            filter.AddRule("Reject recursive urls", StandardUrlFilterRules.RejectRecursiveUrls);

            filter.AddRule("Already visited", StandardUrlFilterRules.MakeRejectUrlInBlacklistRule(visitedUrlBlacklist));

            filter.AddRule("Must be on 4chan.org", StandardUrlFilterRules.MakeUrlMustContainRule("4chan.org"));

            return filter;
        }

        static void SpawnScraper()
        {
            IAsyncEmailAndUrlListProvider scraper = IOCContainer.Instance.Resolve<IAsyncEmailAndUrlListProvider>();

            s_scraperCount++;

            scraper.GetEmailListAsync(HandleEmailList, HandleError<IAsyncEmailListProvider>);
            scraper.GetUrlListAsync(HandleUrlList, HandleError<IAsyncUrlListProvider>);
        }

        static void HandleEmailList(IList<EmailAddress> emails, IAsyncEmailListProvider provider)
        {
            s_logger.DebugFormat("Email list received from scraper ({0})", provider.Source);
            foreach (EmailAddress email in emails)
            {
                lock (s_emails)
                {
                    if (!s_emails.Contains(email.Email))
                    {
                        s_emails.Add(email.Email);
                        s_emailLogger.InfoFormat("Found email {0} at {1}", email.Email, provider.Source);
                    }
                }
            }
        }

        static void HandleUrlList(IList<Uri> urls, IAsyncUrlListProvider provider)
        {
            s_logger.DebugFormat("Url list received from scraper ID ", provider.Source);
            foreach (Uri url in urls)
            {
                if (s_urlFilter.IsUrlValid(url))
                {
                    s_urlLogger.Debug("Accepting URL: " + url.AbsoluteUri);
                    urlProvider.EnqueueUrl(url);
                    s_visitedUrls.AddUrlToBlacklist(url); //Flag that we've already visited this url
                }
                else
                {
                    s_urlLogger.Debug("Rejecting URL: " + url.AbsoluteUri);
                }
            }

            s_scraperCount--;
        }

        static void HandleError<T>(Exception ex, T provider)
        {
            s_logger.ErrorFormat("Exception occurred on scraper ({0}): {1}", (provider as WebpageScraper).Source, ex.Message);
            s_scraperCount--;
        }
    }
}
