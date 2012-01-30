using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.Crawler.EmailFetching
{
    public class EmailAddress
    {
        public string Email { get; private set; }

        public EmailAddress(string emailAddress)
        {
            Email = emailAddress;
        }
    }
}
