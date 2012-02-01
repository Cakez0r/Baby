using Baby.Crawler;
using Microsoft.Practices.Unity;
using System.Collections.Generic;
using Baby.Crawler.EmailFetching;
using System;
using Baby.Data;
using System.IO;
using Baby.UrlFiltering;
using System.Threading;

namespace Baby
{
    /// <summary>
    /// Test code until all the components are tested.
    /// Sorry 4chan... totally using you as a testing ground.
    /// </summary>
    class Program
    {
        static InMemoryUrlProvider urlProvider = new InMemoryUrlProvider(new Uri[] { new Uri("http://www.4chan.org") });

        static StreamWriter s_emailLog = new StreamWriter(File.OpenWrite("emails.txt"));
        static StreamWriter s_errorLog = new StreamWriter(File.OpenWrite("errors.txt"));
        static StreamWriter s_urlLog = new StreamWriter(File.OpenWrite("urls.txt"));

        static URLFilter s_urlFilter;
        static IUrlBlacklist s_visitedUrls;

        const int SCRAPER_LIMIT = 50;
        static int s_scraperCount = 0;

        static void Main(string[] args)
        {
            //Test!
            IOCContainer.Instance.RegisterInstance<IUrlProvider>(urlProvider);

            s_visitedUrls = IOCContainer.Instance.Resolve<IUrlBlacklist>();

            s_urlFilter = MakeUrlFilter(s_visitedUrls);

            SpawnScraper();

            Console.WriteLine("Get ready...");

            while (true)
            {
                if (s_scraperCount < SCRAPER_LIMIT)
                {
                    SpawnScraper();
                }

                Thread.Sleep(50);
            }
        }

        static URLFilter MakeUrlFilter(IUrlBlacklist visitedUrlBlacklist)
        {
            URLFilter filter = new URLFilter();

            //Probably a better way to do this with Accept header...
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("exe"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("css"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("png"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("gif"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("jpg"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("zip"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("7z"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("rar"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("gz"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("avi"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("mpg"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("mp3"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("pdf"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("dmg"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("iso"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("ico"));
            filter.AddRule(StandardUrlFilterRules.MakeRejectExtensionRule("rss"));

            filter.AddRule(StandardUrlFilterRules.RejectHashUrls);

            filter.AddRule(StandardUrlFilterRules.RejectJavascriptUrl);

            filter.AddRule(StandardUrlFilterRules.RejectRecursiveUrls);

            filter.AddRule(StandardUrlFilterRules.MakeRejectUrlInBlacklistRule(visitedUrlBlacklist));

            filter.AddRule(StandardUrlFilterRules.MakeUrlMustContainRule("4chan.org"));

            return filter;
        }

        static void SpawnScraper()
        {
            IAsyncEmailAndUrlListProvider scraper = IOCContainer.Instance.Resolve<IAsyncEmailAndUrlListProvider>();

            s_scraperCount++;

            scraper.GetEmailListAsync(HandleEmailList, HandleError);
            scraper.GetUrlListAsync(HandleUrlList, HandleError);
        }

        static void HandleEmailList(IList<EmailAddress> emails)
        {
            foreach (EmailAddress email in emails)
            {
                LogEmail(email);
            }
        }

        static void HandleUrlList(IList<Uri> urls)
        {
            //Well this is going to chew ALL of your system resources... but fun :)
            foreach (Uri url in urls)
            {
                if (s_urlFilter.IsUrlValid(url))
                {
                    LogUrl(url);
                    urlProvider.EnqueueUrl(url);
                    s_visitedUrls.AddUrlToBlacklist(url); //Flag that we've already visited this url
                }
            }

            s_scraperCount--;
        }

        static void HandleError(Exception ex)
        {
            LogError(ex);
            s_scraperCount--;
        }

        static void LogUrl(Uri url)
        {
            string message = "Found a url: " + url.AbsoluteUri;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            s_urlLog.WriteLine(message);
            s_urlLog.Flush();
        }

        static void LogEmail(EmailAddress email)
        {
            string message = "Found an email: " + email.Email;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            s_emailLog.WriteLine(message);
            s_emailLog.Flush();
        }

        static void LogError(Exception error)
        {
            string message = "Encountered an error: " + error.Message;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = ConsoleColor.White;
            s_errorLog.WriteLine(message);
            s_errorLog.Flush();
        }
    }
}
