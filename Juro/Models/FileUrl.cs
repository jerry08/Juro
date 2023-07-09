using System.Collections.Generic;

namespace Juro.Models
{
    public class FileUrl
    {
        public string Url { get; set; } = default!;

        public Dictionary<string, string> Headers { get; set; } = new();

        public FileUrl()
        {
        }

        public FileUrl(string url)
        {
            Url = url;
        }

        public FileUrl(string url, Dictionary<string, string> headers)
        {
            Url = url;
            Headers = headers;
        }
    }
}