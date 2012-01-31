using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Net;
using Baby.Shared;
using Baby.Crawler.PageFetching;
using Baby.Crawler.EmailFetching;
using Baby.Data;

namespace Baby.Crawler
{
    /// <summary>
    /// Parses a page for links and email addresses
    /// </summary>
    public class WebpageScraper : IAsyncEmailAndUrlListProvider
    {
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
        /// Matches base href tags, which specify a prefix for all hrefs
        /// </summary>
        private const string REGEX_MATCH_BASE_HREF = "base href=\"?([a-zA-Z]|[0-9]|\\(|\\)|\\*|=|,|;|\\$|!|~|#|@|\\.|/|:|&|\\?|\\+|\\.|-|_)+\"?";

        /// <summary>
        /// Matches a link url
        /// </summary>
        private const string REGEX_MATCH_URL = "href=\"?([a-zA-Z]|[0-9]|\\(|\\)|\\*|=|,|;|\\$|!|~|#|@|\\.|/|:|&|\\?|\\+|\\.|-|_)+\"?";

        /// <summary>
        /// Matches an email address
        /// </summary>
        private const string REGEX_MATCH_EMAIL = "([a-zA-Z]|[0-9]|\\.|-|_)+@([a-zA-Z]+)\\.(com|co\\.uk)";

        /// <summary>
        /// Empty lists to use where needed, so we don't go nuts on memory allocation
        /// </summary>
        private static readonly ReadOnlyCollection<Uri> s_emptyUriList = new List<Uri>().AsReadOnly();
        private static readonly ReadOnlyCollection<EmailAddress> s_emptyEmailList = new List<EmailAddress>().AsReadOnly();

        /// <summary>
        /// Link matching regex
        /// </summary>
        private static readonly Regex s_urlRegex;

        /// <summary>
        /// Email matching regex
        /// </summary>
        private static readonly Regex s_emailRegex;

        /// <summary>
        /// Base href matching regex
        /// </summary>
        private static readonly Regex s_baseHrefRegex;


        /// <summary>
        /// The url we start searching at
        /// </summary>
        public Uri Url
        {
            get;
            private set;
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
        /// Initialize statics
        /// </summary>
        static WebpageScraper()
        {
            s_urlRegex = new Regex(REGEX_MATCH_URL, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            s_emailRegex = new Regex(REGEX_MATCH_EMAIL, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            s_baseHrefRegex = new Regex(REGEX_MATCH_BASE_HREF, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Create a new web scraper.
        /// </summary>
        /// <param name="url">The URL to begin traversing at</param>
        public WebpageScraper(IUrlProvider urlProvider, IAsyncWebpageProvider webpageProvider)
        {
            Url = urlProvider.GetUri();
            m_webpageProvider = webpageProvider;
            State = WebpageScraperState.Idle;
        }

        /// <summary>
        /// Start scraping!
        /// </summary>
        private void Scrape()
        {
            //Only start if scraper hasn't already ran
            if (State == WebpageScraperState.Idle)
            {
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
                    HandleFailedDownload(new Exception("URL Provider failed to provide a URL to scrape."));
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
        private void HandleSuccessfulDownload(string html)
        {
            //Update list of links
            Urls = ExtractLinks(html);

            //Update list of emails
            Emails = ExtractEmails(html);

            //Update state
            State = WebpageScraperState.FinishedSuccess;

            //Notify of completion
            if (ScrapeCompletionCallback != null)
            {
                ScrapeCompletionCallback(this);
            }
        }

        /// <summary>
        /// Handle any errors if a web page failed to download
        /// </summary>
        private void HandleFailedDownload(Exception ex)
        {
            //Set the exception that occurred
            Error = ex;

            //Update state
            State = WebpageScraperState.FinishedError;

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
        private List<Uri> ExtractLinks(string html)
        {
            List<Uri> links = new List<Uri>();

            //See if this page has a base href specified
            string baseUrl = null;
            Match baseUrlMatch = Regex.Match(html, REGEX_MATCH_BASE_HREF);
            if (baseUrlMatch.Success)
            {
                baseUrl = StripBaseHrefGarbage(baseUrlMatch.Value);
            }

            Match match = null;
            int startIndex = 0;

            //Start iterating matches for links on the page
            while ((match = s_urlRegex.Match(html, startIndex)).Success)
            {
                //Remove any junk captured by the regex
                string link = StripLinkGarbage(match.Value);

                //Handle special links
                if (link.StartsWith("mailto:"))
                {
                    //TODO: Pick this up with ExtractEmails
                    startIndex += 7;
                    continue;
                }
                else if (link.StartsWith("javascript:"))
                {
                    //Ignore JS
                    startIndex += 11;
                    continue;
                }
                else if (link.StartsWith("#"))
                {
                    //Ignore # links (links to the same page!)
                    startIndex++;
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
                    links.Add(new Uri(link));
                }
                catch
                {
                    
                }
                startIndex = match.Index + 1;
            }

            return links;
        }

        /// <summary>
        /// Parse any emails found on a page
        /// </summary>
        /// <param name="html">The html source code of the page</param>
        /// <returns>A list of email addresses found on the page</returns>
        private static List<EmailAddress> ExtractEmails(string html)
        {
            List<EmailAddress> emails = new List<EmailAddress>();

            //Iterate matches for email addresses
            Match match = null;
            int startIndex = 0;
            while ((match = s_emailRegex.Match(html, startIndex)).Success)
            {
                //Add the email to the results and keep scanning
                emails.Add(new EmailAddress(match.Value));
                startIndex = match.Index + match.Value.Length;
            }

            return emails;
        }

        /// <summary>
        /// Helper to remove any extra garbage captured by the link regex
        /// </summary>
        private static string StripLinkGarbage(string link)
        {
            return link.Substring(5).Trim('"');
        }

        /// <summary>
        /// Helper to remove any extra garbage captured by the base href regex
        /// </summary>
        private static string StripBaseHrefGarbage(string baseHref)
        {
            return baseHref.Substring(10).Trim('"');
        }

        /// <summary>
        /// Helper function to call the async callbacks when a scrape completes
        /// </summary>
        private static void DispatchCallbackForScraper<T>(WebpageScraper scraper, Action<T> completionCallback, Action<Exception> errorCallback, T completionParameter)
        {
            if (scraper.State == WebpageScraperState.FinishedError)
            {
                //Dispatch error callback
                if (errorCallback != null)
                {
                    errorCallback(scraper.Error);
                }
            }
            else if (scraper.State == WebpageScraperState.FinishedSuccess)
            {
                //Dispatch success callback
                if (completionCallback != null)
                {
                    completionCallback(completionParameter);
                }
            }
        }

        /// <summary>
        /// Begins an asynchronous get of urls.
        /// </summary>
        public void GetUrlListAsync(Action<IList<Uri>> completionCallback, Action<Exception> errorCallback)
        {
            if (State == WebpageScraperState.FinishedError || State == WebpageScraperState.FinishedSuccess)
            {
                //If we completed, then run callbacks
                DispatchCallbackForScraper<IList<Uri>>(this, completionCallback, errorCallback, this.Urls);
            }
            else
            {
                //If we haven't completed, hook up any callbacks
                ScrapeCompletionCallback += (scraper) => DispatchCallbackForScraper<IList<Uri>>(this, completionCallback, errorCallback, this.Urls);
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
        public void GetEmailListAsync(Action<IList<EmailAddress>> completionCallback, Action<Exception> errorCallback)
        {
            if (State == WebpageScraperState.FinishedError || State == WebpageScraperState.FinishedSuccess)
            {
                //If we completed, then run callbacks
                DispatchCallbackForScraper<IList<EmailAddress>>(this, completionCallback, errorCallback, this.Emails);
            }
            else
            {
                //If we haven't completed, hook up any callbacks
                ScrapeCompletionCallback += (scraper) => DispatchCallbackForScraper<IList<EmailAddress>>(this, completionCallback, errorCallback, this.Emails);
            }

            if (State == WebpageScraperState.Idle)
            {
                //If we're not scraping yet, then kick everything off
                Scrape();
            }
        }
    }
}
