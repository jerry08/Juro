using Juro.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juro.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProivdersController : ControllerBase
{
    [HttpGet]
    public IEnumerable<Provider> Get(ProviderType type) =>
        Program.Providers.Where(x => x.Type == type);
}
