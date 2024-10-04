using Juro.Core.Models.Manga;
using Juro.Core.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers.Manga;

[ApiController]
[Route("api/[controller]")]
public class MangaBaseController(IMangaProvider provider) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IMangaInfo> GetAsync(string id)
    {
        id = Uri.UnescapeDataString(id);

        return await provider.GetMangaInfoAsync(id);
    }

    [HttpGet("Search")]
    public async Task<IEnumerable<IMangaResult>> SearchAsync([FromQuery(Name = "q")] string query)
    {
        query = Uri.UnescapeDataString(query);

        return await provider.SearchAsync(query);
    }

    [HttpGet]
    [Route("ChapterPages/{id}")]
    public async Task<IEnumerable<IMangaChapterPage>> GetChapterPagesAsync(string id)
    {
        id = Uri.UnescapeDataString(id);

        return await provider.GetChapterPagesAsync(id);
    }
}
