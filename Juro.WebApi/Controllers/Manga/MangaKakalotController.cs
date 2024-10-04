using Juro.Providers.Manga;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers.Manga;

[ApiController]
[Route("api/[controller]")]
public class MangaKakalotController(MangaKakalot provider) : MangaBaseController(provider)
{
}
