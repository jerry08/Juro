using Juro.Providers.Anime;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers.Anime;

[ApiController]
[Route("api/[controller]")]
public class AnimePaheController(AnimePahe provider) : AnimeBaseController(provider)
{
}
