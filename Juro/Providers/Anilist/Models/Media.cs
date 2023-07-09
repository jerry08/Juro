using System.Collections.Generic;
using Juro.Models.Anime;
using Juro.Providers.Anilist.Api;
using ApiMedia = Juro.Providers.Anilist.Api.Media;

namespace Juro.Providers.Anilist.Models
{
    public class Media
    {
        public AnimeInfo? Anime { get; set; }
        //public MangaInfo? Manga { get; set; }

        public int Id { get; set; }

        public int? IdMal { get; set; }
        public string? TypeMAL { get; set; }

        public string? Name { get; set; }
        public string? NameRomaji { get; set; }
        public string? UserPreferredName { get; set; }

        public Studio? MainStudio { get; set; }

        public string? Cover { get; set; }
        public string? Banner { get; set; }
        public string? Relation { get; set; }
        public int? Popularity { get; set; }

        public bool IsAdult { get; set; }
        public bool IsFav { get; set; }
        public bool Notify { get; set; }

        public int? UserListId { get; set; }
        public bool IsListPrivate { get; set; }
        public int? UserProgress { get; set; }
        public string? UserStatus { get; set; }
        public int UserScore { get; set; }
        public int UserRepeat { get; set; }
        public long? UserUpdatedAt { get; set; }
        public FuzzyDate UserStartedAt { get; set; } = default!;

        public FuzzyDate UserCompletedAt { get; set; } = default!;

        //public InCustomListsOf: MutableMap<String, Boolean>?= null,
        public int? UserFavOrder { get; set; }

        public string? Status { get; set; }
        public string? Format { get; set; }
        public string? Source { get; set; }
        public string? CountryOfOrigin { get; set; }
        public int? MeanScore { get; set; }
        public List<string> Genres { get; set; } = default!;
        public List<string> Tags { get; set; } = default!;
        public string? Description { get; set; }
        public List<string> Synonyms { get; set; } = default!;
        public string? Trailer { get; set; }
        public FuzzyDate? StartDate { get; set; }
        public FuzzyDate? EndDate { get; set; }

        public List<Character>? Characters { get; set; }
        public Media? Prequel { get; set; }
        public Media? Sequel { get; set; }
        public List<Media>? Relations { get; set; }
        public List<Media>? Recommendations { get; set; }

        public string? VrvId { get; set; }
        public string? CrunchySlug { get; set; }

        public string? NameMAL { get; set; }
        public string? ShareLink { get; set; }

        public Selected? Selected { get; set; }

        public bool CameFromContinue { get; set; }

        public Media(ApiMedia apiMedia)
        {
            Id = apiMedia.Id;
            IdMal = apiMedia.IdMal;
            Popularity = apiMedia.Popularity;
            Name = apiMedia.Title!.English;
            NameRomaji = apiMedia.Title!.Romaji;
            UserPreferredName = apiMedia.Title!.UserPreferred;
            Cover = apiMedia.CoverImage?.Large;
            Banner = apiMedia.BannerImage;
            Status = apiMedia.Status.ToString();
            IsFav = apiMedia.IsFavourite ?? false;
            IsAdult = apiMedia.IsAdult ?? false;
            IsListPrivate = apiMedia.MediaListEntry?.IsPrivate ?? false;
            UserProgress = apiMedia.MediaListEntry?.Progress;
            UserScore = (int?)apiMedia.MediaListEntry?.Score ?? 0;
            UserStatus = apiMedia.MediaListEntry?.Status?.ToString();
            MeanScore = apiMedia.MeanScore;

            //Todo: Cater for anime and manga
            Anime = apiMedia.Type == MediaType.Anime ? new AnimeInfo() : null;
        }

        public Media(MediaList mediaList)
        {
        }

        public Media(MediaEdge mediaEdge)
        {
            Relation = mediaEdge.RelationType?.ToString();
        }
    }
}