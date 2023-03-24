namespace Juro.Providers.Anilist.Models;

public class Selected
{
    public int Window { get; set; }
    public int? RecyclerStyle { get; set; }
    public bool RecyclerReversed { get; set; }
    public int Chip { get; set; }
    public int Source { get; set; }
    public bool PreferDub { get; set; }
    public string? Server { get; set; }
    public int Video { get; set; }
}