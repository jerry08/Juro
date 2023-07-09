namespace Juro.Providers.Anilist
{
    public class AnilistQueries
    {
        public static string Trending(int page, int perPage, string type)
        {
            return
                "query ($page: Int = ${page}, $id: Int, $type: MediaType = ${type}, $isAdult: Boolean = false, $size: Int = ${perPage}, $sort: [MediaSort] = [TRENDING_DESC, POPULARITY_DESC]) { Page(page: $page, perPage: $size) { pageInfo { total perPage currentPage lastPage hasNextPage } media(id: $id, type: $type, isAdult: $isAdult, sort: $sort) { id idMal status(version: 2) title { userPreferred romaji english native } genres trailer { id site thumbnail } description format bannerImage coverImage{ extraLarge large medium color } episodes meanScore duration season seasonYear averageScore nextAiringEpisode { airingAt timeUntilAiring episode }  } } }"
                    .Replace("${page}", $"{page}")
                    .Replace("${perPage}", $"{perPage}")
                    .Replace("${type}", $"{type}");
        }
    }
}