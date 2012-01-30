using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Shared
{
    public class UrlHelpers
    {
        public static bool IsRelativeUrl(string url)
        {
            return !url.StartsWith("http");
        }

        public static string MakeAbsoluteUrl(Uri origin, string relativePath)
        {
            string baseUrl = origin.Scheme + "://" + origin.Host + "/";

            string absolute = baseUrl;

            if (relativePath[0] == '/')
            {
                absolute += relativePath.Substring(1);
            }
            else
            {
                int segmentModifier = origin.AbsoluteUri.EndsWith("/") ? 0 : -1;
                for (int i = 1; i < origin.Segments.Length + segmentModifier; i++)
                {
                    absolute += origin.Segments[i];
                }

                absolute += relativePath;
            }

            //Remove any retardedness
            absolute = absolute.Replace("/./", "/");

            return absolute;
        }
    }
}
