using Juro.Providers.Anime.Indonesian;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers.Anime;

[ApiController]
[Route("api/[controller]")]
public class OtakuDesuController(OtakuDesu provider) : AnimeBaseController(provider)
{
}
