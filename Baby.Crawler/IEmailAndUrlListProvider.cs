using Baby.Crawler.EmailFetching;
using Baby.Crawler.PageFetching;

namespace Baby.Crawler
{
    public interface IAsyncEmailAndUrlListProvider : IAsyncEmailListProvider, IAsyncUrlListProvider
    {
    }
}
