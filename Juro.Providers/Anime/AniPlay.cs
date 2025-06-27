using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Juro.Core;
using Juro.Core.Converters;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Juro.Core.Utils;

namespace Juro.Providers.Anime;

/// <summary>
/// Client for interacting with AnimePahe.
/// </summary>
/// <remarks>
/// Initializes an instance of <see cref="AniPlay"/>.
/// </remarks>
public class AniPlay(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public bool IsDubAvailableSeparately => throw new NotImplementedException();

    public string Key => Name;

    public string Name => "AniPlay";

    public string Language => "en";

    public string BaseUrl => "https://aniplaynow.live";

    /// <summary>
    /// Initializes an instance of <see cref="AniPlay"/>.
    /// </summary>
    public AniPlay(Func<HttpClient> httpClientProvider)
        : this(new HttpClientFactory(httpClientProvider)) { }

    /// <summary>
    /// Initializes an instance of <see cref="AniPlay"/>.
    /// </summary>
    public AniPlay()
        : this(Http.ClientProvider) { }

    public ValueTask<List<IAnimeInfo>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException(
            "AniPlay uses Anilist for searching. Use AnilistClient instead."
        );
    }

    public ValueTask<IAnimeInfo> GetAnimeInfoAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public async ValueTask<List<Episode>> GetEpisodesAsync(
        string animeId,
        CancellationToken cancellationToken = default
    )
    {
        var client = httpClientFactory.CreateClient();

        var requestBody = @$"[""{animeId}"",true,false]";
        var content = new StringContent(requestBody, Encoding.UTF8, "text/plain");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/anime/info/{animeId}")
        {
            Content = content,
        };

        // Add headers
        request.Headers.Add("Next-Action", GetHeaderValue("domain1", "NEXT_ACTION_EPISODE_LIST"));

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var episodesArrayString = ExtractEpisodeList(responseString);
        if (episodesArrayString is null)
        {
            return [];
        }

        var list = JsonSerializer.Deserialize<List<AniPlayProviderModel>>(episodesArrayString);

        return list?.SelectMany(x =>
                    x.Episodes.Select(x => new Episode()
                    {
                        Id = x.Id,
                        Number = x.Number,
                        Name = x.Title,
                        Image = x.Image,
                        Description = x.Description,
                    })
                )
                ?.ToList() ?? [];
    }

    public ValueTask<List<VideoServer>> GetVideoServersAsync(
        string episodeId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    public ValueTask<List<VideoSource>> GetVideosAsync(
        VideoServer server,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    private static string? ExtractEpisodeList(string input)
    {
        return ExtractList(input, '[', ']');
    }

    private static string? ExtractSourcesList(string input)
    {
        return ExtractList(input, '{', '}');
    }

    private static string? ExtractList(string input, char bracket1, char bracket2)
    {
        var startMarker = $"1:{bracket1}";
        var list1Index = input.IndexOf(startMarker);
        if (list1Index == -1)
            return null;

        var startIndex = list1Index + startMarker.Length;
        var endIndex = startIndex;
        var bracketCount = 1;

        while (endIndex < input.Length && bracketCount > 0)
        {
            switch (input[endIndex])
            {
                case char c when c == bracket1:
                    bracketCount++;
                    break;
                case char c when c == bracket2:
                    bracketCount--;
                    break;
            }
            endIndex++;
        }

        return bracketCount == 0
            ? input.Substring(startIndex - 1, endIndex - startIndex + 1)
            : null;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> HEADER_NEXT_ACTION =
        new()
        {
            {
                "domain1",
                new Dictionary<string, string>
                {
                    { "NEXT_ACTION_EPISODE_LIST", "f3422af67c84852f5e63d50e1f51718f1c0225c4" },
                    { "NEXT_ACTION_SOURCES_LIST", "5dbcd21c7c276c4d15f8de29d9ef27aef5ea4a5e" },
                }
            },
            {
                "domain2",
                new Dictionary<string, string>
                {
                    { "NEXT_ACTION_EPISODE_LIST", "56e4151352ded056cbe226d2376c7436cffc9a37" },
                    { "NEXT_ACTION_SOURCES_LIST", "8a76af451978c817dde2364326a5e4e45eb43db1" },
                }
            },
        };

    private static string GetHeaderValue(string serverHost, string key)
    {
        if (
            HEADER_NEXT_ACTION.ContainsKey(serverHost)
            && HEADER_NEXT_ACTION[serverHost].ContainsKey(key)
        )
        {
            return HEADER_NEXT_ACTION[serverHost][key];
        }
        else
        {
            throw new Exception("Bad host/key");
        }
    }
}
