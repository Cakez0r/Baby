using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Baby.Crawler.EmailFetching;
using Baby.Crawler.PageFetching;
using Baby.Data;
using Baby.Shared;
using log4net;

namespace Baby.Crawler
{
    /// <summary>
    /// Parses a page for links and email addresses
    /// </summary>
    public class WebpageScraper : IAsyncEmailAndUrlListProvider
    {
        private static ILog s_logger = LogManager.GetLogger(typeof(WebpageScraper));
        private static ILog s_scrapeLogger = LogManager.GetLogger("scrapes");

        /// <summary>
        /// Used to represent the current state of a scraper
        /// </summary>
        public enum WebpageScraperState
        {
            Idle,
            Scraping,
            FinishedSuccess,
            FinishedError,
        }

        /// <summary>
        /// Empty lists to use where needed, so we don't go nuts on memory allocation
        /// </summary>
        private static readonly ReadOnlyCollection<Uri> s_emptyUriList = new List<Uri>().AsReadOnly();
        private static readonly ReadOnlyCollection<EmailAddress> s_emptyEmailList = new List<EmailAddress>().AsReadOnly();

        /// <summary>
        /// Email matching regex
        /// TODO: Make this better...
        /// </summary>
        private static readonly Regex s_emailRegex = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Base href matching regex
        /// </summary>
        private static readonly Regex s_baseHrefRegex = new Regex("base href=\"(?<1>[^\"]*)\"", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);


        /// <summary>
        /// The url we start searching at
        /// </summary>
        public Uri Url
        {
            get;
            private set;
        }

        /// <summary>
        /// The data source of this crawler (for IAsync interfaces)
        /// </summary>
        public string Source
        {
            get { return Url != null ? Url.AbsoluteUri : "[NONE]"; }
        }

        /// <summary>
        /// A list of emails collected from scraping
        /// </summary>
        public IList<EmailAddress> Emails
        {
            get;
            private set;
        }

        /// <summary>
        /// A list of links collected from scraping
        /// </summary>
        public IList<Uri> Urls
        {
            get;
            private set;
        }

        /// <summary>
        /// If anything went wrong whilst scraping, the exception will be here
        /// </summary>
        public Exception Error
        {
            get;
            private set;
        }

        /// <summary>
        /// The current state of this scraper
        /// </summary>
        public WebpageScraperState State
        {
            get;
            private set;
        }

        /// <summary>
        /// A callback that will fire when a scrape is complete
        /// </summary>
        public Action<WebpageScraper> ScrapeCompletionCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Object used for fetching webpages
        /// </summary>
        IAsyncWebpageProvider m_webpageProvider;


        /// <summary>
        /// Create a new web scraper.
        /// </summary>
        /// <param name="url">The URL to begin traversing at</param>
        public WebpageScraper(IUrlProvider urlProvider, IAsyncWebpageProvider webpageProvider)
        {
            Url = urlProvider.GetUri();
            m_webpageProvider = webpageProvider;
            State = WebpageScraperState.Idle;

            s_logger.DebugFormat("Scraper created for url {0}", Url != null ? Url.AbsoluteUri : "NULL URL");
        }

        /// <summary>
        /// Start scraping!
        /// </summary>
        private void Scrape()
        {
            //Only start if scraper hasn't already ran
            if (State == WebpageScraperState.Idle)
            {
                s_scrapeLogger.InfoFormat("Starting scrape of URL {0}", Url != null ? Url.AbsoluteUri : "NULL URL");

                //Don't show any results yet
                Emails = s_emptyEmailList;
                Urls = s_emptyUriList;

                if (Url != null)
                {
                    //Kick off the web page download
                    m_webpageProvider.GetWebpageAsync(Url, HandleSuccessfulDownload, HandleFailedDownload);

                    //Update state
                    State = WebpageScraperState.Scraping;
                }
                else
                {
                    HandleFailedDownload(new Exception("URL Provider failed to provide a URL to scrape."), m_webpageProvider);
                }
            }
            else
            {
                throw new Exception("Tried to start scraping on a scraper that has already finished");
            }
        }

        /// <summary>
        /// Kick off any logic for parsing a page download
        /// </summary>
        /// <param name="html">The html source code of the downloaded page</param>
        private void HandleSuccessfulDownload(string html, IAsyncWebpageProvider provider)
        {
            //Update list of links
            Urls = ExtractLinks(ref html);

            //Update list of emails
            Emails = ExtractEmails(ref html);

            //Update state
            State = WebpageScraperState.FinishedSuccess;

            s_logger.DebugFormat("Sucessfully scraped URL {0}", Url != null ? Url.AbsoluteUri : "NULL URL");

            //Notify of completion
            if (ScrapeCompletionCallback != null)
            {
                ScrapeCompletionCallback(this);
            }
        }

        /// <summary>
        /// Handle any errors if a web page failed to download
        /// </summary>
        private void HandleFailedDownload(Exception ex, IAsyncWebpageProvider provider)
        {
            //Set the exception that occurred
            Error = ex;

            //Update state
            State = WebpageScraperState.FinishedError;

            s_logger.WarnFormat("Failed to scrape URL {0}: {1}", Url != null ? Url.AbsoluteUri : "NULL URL", ex);

            //Notify of completion
            if (ScrapeCompletionCallback != null)
            {
                ScrapeCompletionCallback(this);
            }
        }

        /// <summary>
        /// Parse a web page for any links
        /// </summary>
        /// <param name="html">The html source code for the page</param>
        /// <returns>A list of links found on the page</returns>
        private List<Uri> ExtractLinks(ref string html)
        {
            List<Uri> links = new List<Uri>();

            //See if this page has a base href specified
            string baseUrl = null;
            Match baseUrlMatch = s_baseHrefRegex.Match(html);
            if (baseUrlMatch.Success)
            {
                baseUrl = baseUrlMatch.Groups[1].Value;
            }

            string link;
            int startIndex = 0;

            //Start iterating matches for links on the page
            while ((link = FastLinkMatch.GetNextUrl(ref html, startIndex, out startIndex)) != null)
            {
                //Handle special links
                if (link.StartsWith("mailto:"))
                {
                    //TODO: Pick this up with ExtractEmails
                    continue;
                }
                else if (link.StartsWith("javascript:"))
                {
                    //Ignore JS
                    continue;
                }
                else if (link.StartsWith("#"))
                {
                    //Ignore # links (links to the same page!)
                    continue;
                }
                else if (link.Length == 0)
                {
                    continue;
                }
                else if (link.StartsWith("//"))
                {
                    //Expand links to include protocol type if they start with //
                    link = Url.Scheme + ":" + link;
                }

                //Check if this link is relative or absolute
                if (UrlHelpers.IsRelativeUrl(link))
                {
                    //If the link starts with a / or there was no base href tag for the page
                    if (link[0] == '/' || baseUrl == null)
                    {
                        //Expand the relative url into an absolute one
                        link = UrlHelpers.MakeAbsoluteUrl(Url, link);
                    }
                    else
                    {
                        //Otherwise concat the base href and relative components
                        link = baseUrl + link;
                    }
                }

                //Add the link to the results list if it is a valid Uri and move on
                try
                {
                    Uri newUrl = new Uri(link);
                    links.Add(newUrl);
                    s_logger.DebugFormat("Found link {0} on url {1}", newUrl.AbsoluteUri, Url.AbsoluteUri);
                }
                catch
                {
                    s_logger.DebugFormat("Matched an invalid link {0} found on url {1}", link, Url.AbsoluteUri);
                }
            }

            return links;
        }

        /// <summary>
        /// Parse any emails found on a page
        /// </summary>
        /// <param name="html">The html source code of the page</param>
        /// <returns>A list of email addresses found on the page</returns>
        private List<EmailAddress> ExtractEmails(ref string html)
        {
            List<EmailAddress> emails = new List<EmailAddress>();

            //Iterate matches for email addresses
            //Match match = null;
            //int startIndex = 0;
            MatchCollection matches = s_emailRegex.Matches(html);
            foreach (Match match in matches)
            {
                //Add the email to the results and keep scanning
                emails.Add(new EmailAddress(match.Value));

                s_logger.DebugFormat("Found an email {0} on url {1}", match.Value, Url.AbsoluteUri);
            }

            return emails;
        }

        /// <summary>
        /// Helper function to call the async callbacks when a scrape completes
        /// </summary>
        private static void DispatchCallbackForScraper<T, U>(WebpageScraper scraper, Action<T, U> completionCallback, Action<Exception, U> errorCallback, T completionParameter, U provider)
        {
            if (scraper.State == WebpageScraperState.FinishedError)
            {
                //Dispatch error callback
                if (errorCallback != null)
                {
                    errorCallback(scraper.Error, provider);
                }
            }
            else if (scraper.State == WebpageScraperState.FinishedSuccess)
            {
                //Dispatch success callback
                if (completionCallback != null)
                {
                    completionCallback(completionParameter, provider);
                }
            }
        }

        /// <summary>
        /// Begins an asynchronous get of urls.
        /// </summary>
        public void GetUrlListAsync(Action<IList<Uri>, IAsyncUrlListProvider> completionCallback, Action<Exception, IAsyncUrlListProvider> errorCallback)
        {
            if (State == WebpageScraperState.FinishedError || State == WebpageScraperState.FinishedSuccess)
            {
                //If we completed, then run callbacks
                DispatchCallbackForScraper<IList<Uri>, IAsyncUrlListProvider>(this, completionCallback, errorCallback, this.Urls, this);
            }
            else
            {
                //If we haven't completed, hook up any callbacks
                ScrapeCompletionCallback += (scraper) => DispatchCallbackForScraper<IList<Uri>, IAsyncUrlListProvider>(this, completionCallback, errorCallback, this.Urls, this);
            }

            if (State == WebpageScraperState.Idle)
            {
                //If we're not scraping yet, then kick everything off
                Scrape();
            }
        }

        /// <summary>
        /// Begins an asynchronous get of email addresses
        /// </summary>
        public void GetEmailListAsync(Action<IList<EmailAddress>, IAsyncEmailListProvider> completionCallback, Action<Exception, IAsyncEmailListProvider> errorCallback)
        {
            if (State == WebpageScraperState.FinishedError || State == WebpageScraperState.FinishedSuccess)
            {
                //If we completed, then run callbacks
                DispatchCallbackForScraper<IList<EmailAddress>, IAsyncEmailListProvider>(this, completionCallback, errorCallback, this.Emails, this);
            }
            else
            {
                //If we haven't completed, hook up any callbacks
                ScrapeCompletionCallback += (scraper) => DispatchCallbackForScraper<IList<EmailAddress>, IAsyncEmailListProvider>(this, completionCallback, errorCallback, this.Emails, this);
            }

            if (State == WebpageScraperState.Idle)
            {
                //If we're not scraping yet, then kick everything off
                Scrape();
            }
        }
    }
}
