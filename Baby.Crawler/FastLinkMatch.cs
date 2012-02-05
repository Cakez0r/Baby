using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Crawler
{
    public unsafe class FastLinkMatch
    {
        private const string href_string_lc = "href";
        private const string href_string_uc = "HREF";
        private const string href_suffix_string = "=\"";

        /// <summary>
        /// Finds a URL in some html
        /// </summary>
        public static string GetNextUrl(ref string html, int startAt, out int continueFrom)
        {
            //Pin some objects so we can get their address
            fixed (char* href_ptr_lc = href_string_lc, href_ptr_uc = href_string_uc, href_suffix_ptr = href_suffix_string, html_start_ptr = html)
            {
                //Convert strings into ints (strings are unicode, I.E. sizeof(char) == 2)
                UInt64 href_long_lc = *(UInt64*)href_ptr_lc; //href
                UInt64 href_long_uc = *(UInt64*)href_ptr_uc; //HREF
                UInt32 href_suffix_int = *(UInt32*)href_suffix_ptr; //="

                //Stop scanning when we reach this address
                char* html_end_ptr = html_start_ptr + html.Length - 8;

                //Initialize a pointer to scan through the html
                char* html_current_ptr = html_start_ptr + startAt;

                //Don't scan past the end of the buffer
                while (html_current_ptr <= html_end_ptr)
                {
                    //Convert the html at the scan pointer's current location to a 64 bit int
                    UInt64 current_long = *(UInt64*)html_current_ptr;

                    //If a href is found...
                    if (current_long == href_long_lc || current_long == href_long_uc)
                    {
                        //skip past the href text
                        html_current_ptr += 4;

                        //Now check if it is proceeded by ="
                        if (*(UInt32*)html_current_ptr == href_suffix_int)
                        {
                            //skip past the =" text
                            html_current_ptr += 2;

                            //Take note of where the url starts
                            char* url_start_ptr = html_current_ptr;

                            //Scan forwards until we hit the closing " for the url, or reach the end of the buffer
                            while (*html_current_ptr != '"' && html_current_ptr <= html_end_ptr)
                            {
                                html_current_ptr++;
                            }

                            //Output where we got to in the search, so that the next link can be found
                            continueFrom = (int)(html_current_ptr - html_start_ptr);

                            //copy the url to a new string
                            return new string(url_start_ptr, 0, (int)(html_current_ptr - url_start_ptr)); ;
                        }
                    }

                    //Keep scanning until we find a href or reach the end of the buffer
                    html_current_ptr++;
                }
            }

            //End of the buffer reached, no hrefs found
            continueFrom = 0;
            return null;
        }
    }
}
