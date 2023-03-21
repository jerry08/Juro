using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;

namespace Juro.Extractors;

public interface IVideoExtractor
{
    public string ServerName { get; }

    Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default);
}