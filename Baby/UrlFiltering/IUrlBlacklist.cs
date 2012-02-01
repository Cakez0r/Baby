using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Baby.UrlFiltering
{
    public interface IUrlBlacklist
    {
        bool IsUrlBlacklisted(Uri url);

        void AddUrlToBlacklist(Uri url);
    }
}
