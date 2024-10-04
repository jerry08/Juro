namespace Juro.Core.Models;

public class Provider
{
    public string Key { get; set; } = default!;

    public string Name { get; set; } = default!;

    public ProviderType Type { get; set; }
}
