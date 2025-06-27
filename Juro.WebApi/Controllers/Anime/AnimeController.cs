using Juro.Providers.Anime;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers.Anime;

[ApiController]
[Route("api/[controller]")]
[Obsolete("Gogoanime/Anitaku is officially dead.")]
public class AnimeController(Gogoanime provider) : GogoanimeController(provider)
{
}
