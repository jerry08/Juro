using System;
using System.Net.Http;
using Juro.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with Hentai Stream.
/// </summary>
public class HentaiStream : HentaiFF
{
    public override string Name => "Hentai Stream";

    public override string BaseUrl => "https://hentaistream.moe";

    /// <summary>
    /// Initializes an instance of <see cref="HentaiStream"/>.
    /// </summary>
    public HentaiStream(Func<HttpClient> httpClientProvider) : base(httpClientProvider)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="HentaiStream"/>.
    /// </summary>
    public HentaiStream() : this(Http.ClientProvider)
    {
    }
}