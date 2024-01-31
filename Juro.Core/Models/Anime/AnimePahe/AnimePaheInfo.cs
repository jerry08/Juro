namespace Juro.Core.Models.Anime.AnimePahe;

/// <inheritdoc />
public class AnimePaheInfo : AnimeInfo
{
    public int AnilistId { get; set; }

    public int KitsuId { get; set; }

    public int AnidbId { get; set; }

    public int AnimeNewsNetworkId { get; set; }

    public int MalId { get; set; }

    public int Episodes { get; set; }

    public string Season { get; set; } = default!;

    public int Year { get; set; }

    public float Score { get; set; }
}
