using Juro.Core.Models.Anime;
using Juro.Core.Models.Videos;
using Juro.Core.Providers;
using Microsoft.AspNetCore.Mvc;
using TaskExecutor;

namespace Juro.WebApi.Controllers.Anime;

[ApiController]
[Route("api/[controller]")]
//[ApiExplorerSettings(IgnoreApi = true)]
public class AnimeBaseController(IAnimeProvider animeProvider) : ControllerBase
{
    //[HttpGet]
    //[Route("Get/{id}")]
    [HttpGet("{id}")]
    public async Task<IAnimeInfo> GetAsync(string id)
    {
        id = Uri.UnescapeDataString(id);

        return await animeProvider.GetAnimeInfoAsync(id);
    }

    [HttpGet("Search")]
    public async Task<IEnumerable<IAnimeInfo>> SearchAsync(string query)
    {
        return await animeProvider.SearchAsync(query);
    }

    [HttpGet]
    [Route("Episodes/{id}")]
    public async Task<IEnumerable<Episode>> GetEpisodesAsync(string id)
    {
        id = Uri.UnescapeDataString(id);

        return await animeProvider.GetEpisodesAsync(id);
    }

    [HttpGet]
    [Route("VideoServers/{id}")]
    public async Task<IEnumerable<VideoServer>> GetVideoServersAsync(string id)
    {
        id = Uri.UnescapeDataString(id);

        return await animeProvider.GetVideoServersAsync(id);
    }

    [HttpGet]
    //[Route("Videos{url}")]
    [Route("Videos")]
    public async Task<IEnumerable<VideoSource>> GetVideosAsync([FromQuery(Name = "q")] string query)
    {
        query = Uri.UnescapeDataString(query);

        if (
            Uri.IsWellFormedUriString(query, UriKind.Absolute)
            && !query.Contains("animepahe.ru/play/", StringComparison.OrdinalIgnoreCase)
        )
        {
            var server = new VideoServer(query);
            return await animeProvider.GetVideosAsync(server);
        }

        var servers = await animeProvider.GetVideoServersAsync(query);
        var functions = servers.Select(server =>
            (Func<Task<List<VideoSource>>>)(
                async () =>
                {
                    var list = new List<VideoSource>();

                    try
                    {
                        list.AddRange(await animeProvider.GetVideosAsync(server));
                    }
                    catch { }

                    return list;
                }
            )
        );

        var results = await TaskEx.Run(functions, 5);

        return results.SelectMany(x => x);
    }
}
