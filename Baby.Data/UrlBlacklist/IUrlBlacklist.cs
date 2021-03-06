﻿using System;

namespace Baby.Data
{
    public interface IUrlBlacklist
    {
        string Name { get; set; }

        bool IsUrlBlacklisted(Uri url);

        void AddUrlToBlacklist(Uri url);
    }
}
