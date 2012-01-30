using Baby.Crawler;
using Microsoft.Practices.Unity;
using System.Collections.Generic;
using Baby.Crawler.EmailFetching;
using System;
using Baby.Data;
using System.IO;

namespace Baby
{
    class Program
    {
        static InMemoryUrlProvider urlProvider = new InMemoryUrlProvider(new Uri[] { new Uri("http://www.4chan.org") });

        static StreamWriter s_emailLog = new StreamWriter(File.OpenWrite("emails.txt"));
        static StreamWriter s_errorLog = new StreamWriter(File.OpenWrite("errors.txt"));
        static StreamWriter s_urlLog = new StreamWriter(File.OpenWrite("urls.txt"));

        static void Main(string[] args)
        {
            //Test!
            IOCContainer.Instance.RegisterInstance<IUrlProvider>(urlProvider);

            SpawnScraper();

            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static void SpawnScraper()
        {
            IAsyncEmailAndUrlListProvider scraper = IOCContainer.Instance.Resolve<IAsyncEmailAndUrlListProvider>();

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
                LogUrl(url);
                urlProvider.EnqueueUrl(url);
                SpawnScraper(); //traverse THE INTERNET! Obviously going to fail miserably when it runs out of memory, but serves as a test.
            }
        }

        static void HandleError(Exception ex)
        {
            LogError(ex);
        }

        static void LogUrl(Uri url)
        {
            string message = "Found a url: " + url.AbsoluteUri;
            Console.WriteLine(message);
            s_urlLog.WriteLine(message);
            s_urlLog.Flush();
        }

        static void LogEmail(EmailAddress email)
        {
            string message = "Found an email: " + email.Email;
            Console.WriteLine(message);
            s_emailLog.WriteLine(message);
            s_emailLog.Flush();
        }

        static void LogError(Exception error)
        {
            string message = "Encountered an error: " + error.Message;
            Console.WriteLine(error);
            s_errorLog.WriteLine(message);
            s_errorLog.Flush();
        }
    }
}
