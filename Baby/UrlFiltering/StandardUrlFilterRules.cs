using System;

namespace Baby.UrlFiltering
{
    public static class StandardUrlFilterRules
    {
        /// <summary>
        /// Used in recursion detection heuristic
        /// </summary>
        private const int RECURSION_DETECTION_REPETITION_THRESHOLD = 4;

        /// <summary>
        /// Returns false for URLs that contain a '?'
        /// </summary>
        public static bool RejectDynamicUrls(Uri url)
        {
            return url.Query == string.Empty;
        }

        /// <summary>
        /// Makes a delegate that will reject a url of the given extension
        /// </summary>
        public static Func<Uri, bool> MakeRejectExtensionRule(string extension)
        {
            return (Uri url) => !url.AbsolutePath.EndsWith("." + extension);
        }

        /// <summary>
        /// Returns false for URLs that contain a '#'
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool RejectHashUrls(Uri url)
        {
            return url.Fragment == string.Empty;
        }

        /// <summary>
        /// Returns false for links that would invoke javascript on a page
        /// </summary>
        public static bool RejectJavascriptUrl(Uri url)
        {
            return !url.Scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Attempts to detect a recursive url and reject it.
        /// </summary>
        public static bool RejectRecursiveUrls(Uri url)
        {
            return GetRecurranceCount(url.OriginalString) <= RECURSION_DETECTION_REPETITION_THRESHOLD;
        }

        /// <summary>
        /// Makes a delegate that will return false for urls that are in the specified blacklist
        /// </summary>
        public static Func<Uri, bool> MakeRejectUrlInBlacklistRule(IUrlBlacklist blacklist)
        {
            return (Uri url) => !blacklist.IsUrlBlacklisted(url);
        }

        /// <summary>
        /// Makes a delegate that will only accept urls that contain the given string
        /// </summary>
        public static Func<Uri, bool> MakeUrlMustContainRule(string mustContain)
        {
            return (Uri url) => url.AbsoluteUri.Contains(mustContain);
        }

        /// <summary>
        /// Checks the URL in 8 byte chunks for repetition
        /// </summary>
        private static unsafe int GetRecurranceCount(string url)
        {
            //I possibly got a bit carried away with a micro-optimisation here :/
            //Sue me

            //We want to start at the back of the string
            int strlen = url.Length;

            //We already know there is one occurance of the MIN(strlen, 8) characters of the url
            int recurranceCount = 1;

            //Don't let the garbage collector ruin our day
            fixed (char* str = url)
            {
                //Start at the end of the URL, minus 8 characters
                char* tracker = (char*)(str + strlen - 8);

                //Take note of where we are. We'll compare in 8 byte chunks
                UInt64* checkChunk = (UInt64*)tracker;

                //Move back another 8 bytes
                tracker -= 8;

                //How many bytes we will step back per iteration
                int backtrackStride = 1;

                //Don't go past the start of the string
                while (tracker >= str)
                {
                    //Compare this 8-byte chunk of the string with the ending 8 bytes of the string
                    if (*(UInt64*)tracker == *checkChunk)
                    {
                        //If they are equal and our backtrack stride is 1, we've found a pattern
                        if (backtrackStride == 1)
                        {
                            //See how many bytes we've moved from the end of the string to get to the start of the pattern
                            int bytesMoved = (int)(str + strlen - tracker);

                            //Subtract the size of the end chunk (8 bytes) to give us the total pattern size. 
                            //We can now start stepping through the string in larger chunks
                            backtrackStride = bytesMoved - 8;
                        }

                        //Record one occurrance of the pattern
                        recurranceCount++;
                    }
                    else if (backtrackStride != 1)
                    {
                        //If we aren't looking for the start of the pattern and the chunks don't match
                        //then we've found the end of any recurring patterns, so we can bail out.
                        break;
                    }

                    //Step back through the string
                    tracker -= backtrackStride;
                }
            }

            return recurranceCount;
        }
    }
}
