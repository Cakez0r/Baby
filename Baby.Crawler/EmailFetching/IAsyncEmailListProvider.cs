﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Baby.Crawler.EmailFetching
{
    public interface IAsyncEmailListProvider
    {
        void GetEmailListAsync(Action<IList<EmailAddress>> completionCallback, Action<Exception> errorCallback);
    }
}