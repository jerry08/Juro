using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnimeDl.Utils.Extensions;
using Juro.Providers.Anilist.Api;
using Juro.Providers.Aniskip;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json;
using Character = Juro.Providers.Anilist.Models.Character;
using Media = Juro.Providers.Anilist.Models.Media;
using Studio = Juro.Providers.Anilist.Models.Studio;

namespace Juro.Providers.Anilist;

/// <summary>
/// Client for interacting with Anilist.
/// </summary>
public class AnilistClient
{
    private readonly HttpClient _http;

    /// <summary>
    /// Client for interacting with aniskip api.
    /// </summary>
    public AniskipClient Aniskip { get; }

    public bool IsAdult { get; set; } = false;

    public bool ListOnly { get; set; } = false;

    /// <summary>
    /// Initializes an instance of <see cref="AnilistClient"/>.
    /// </summary>
    public AnilistClient(Func<HttpClient> httpClientProvider)
    {
        _http = httpClientProvider();
        Aniskip = new(httpClientProvider);
    }

    /// <summary>
    /// Initializes an instance of <see cref="AnilistClient"/>.
    /// </summary>
    public AnilistClient() : this(Http.ClientProvider)
    {
    }

    public async ValueTask<T?> ExecuteQueryAsync<T>(string query,
        string variables = "")
    {
        var data = new
        {
            query,
            variables
        };

        var headers = new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" },
            { "Accept", "application/json" }
        };

        var serialized = JsonConvert.SerializeObject(data);

        //var content = new StringContent(query, Encoding.UTF8, "application/json");
        var content = new StringContent(serialized, Encoding.UTF8, "application/json");

        var json = await _http.PostAsync("https://graphql.anilist.co/", headers, content);
        if (!json.StartsWith("{"))
            throw new Exception("Seems like Anilist is down, maybe try using a VPN or you can wait for it to comeback.");

        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task<Media?> GetMediaDetailsAsync(Media media)
    {
        media.CameFromContinue = false;

        var query = "{Media(id:" + media.Id + "){id mediaListEntry{id status score(format:POINT_100) progress private repeat customLists updatedAt startedAt{year month day}completedAt{year month day}}isFavourite siteUrl idMal nextAiringEpisode{episode airingAt}source countryOfOrigin format duration season seasonYear startDate{year month day}endDate{year month day}genres studios(isMain:true){nodes{id name siteUrl}}description trailer { site id } synonyms tags { name rank isMediaSpoiler } characters(sort:[ROLE,FAVOURITES_DESC],perPage:25,page:1){edges{role node{id image{medium}name{userPreferred}}}}relations{edges{relationType(version:2)node{id idMal mediaListEntry{progress private score(format:POINT_100) status} episodes chapters nextAiringEpisode{episode} popularity meanScore isAdult isFavourite title{english romaji userPreferred}type status(version:2)bannerImage coverImage{large}}}}recommendations(sort:RATING_DESC){nodes{mediaRecommendation{id idMal mediaListEntry{progress private score(format:POINT_100) status} episodes chapters nextAiringEpisode{episode}meanScore isAdult isFavourite title{english romaji userPreferred}type status(version:2)bannerImage coverImage{large}}}}externalLinks{url site}}}";

        var response = await ExecuteQueryAsync<Query.Media>(query);
        if (response is not null)
        {
            var fetchedMedia = response?.Data?.Media;
            if (fetchedMedia is null)
                return null;

            media.Source = fetchedMedia.Source.ToString();
            media.CountryOfOrigin = fetchedMedia.CountryOfOrigin;
            media.Format = fetchedMedia.Format.ToString();

            media.StartDate = fetchedMedia.StartDate;
            media.EndDate = fetchedMedia.EndDate;

            if (fetchedMedia.Genres is not null)
            {
                media.Genres = new();
                media.Genres.AddRange(fetchedMedia.Genres);
            }

            media.Trailer = fetchedMedia.Trailer?.Site == "youtube" ?
                 $"https://www.youtube.com/embed/{fetchedMedia.Trailer.Id?.ToString().Trim('"')}" : null;

            if (fetchedMedia.Synonyms is not null)
            {
                media.Synonyms = new();
                media.Synonyms?.AddRange(fetchedMedia.Synonyms);
            }

            if (fetchedMedia.Tags is not null)
            {
                media.Tags = new();

                fetchedMedia.Tags.ForEach(i =>
                {
                    if (i.IsMediaSpoiler == false)
                        media.Tags.Add($"{i.Name} : {i.Rank}%");
                });
            }

            media.Description = fetchedMedia.Description;

            if (fetchedMedia.Characters is not null)
            {
                media.Characters = new();

                fetchedMedia.Characters.Edges?.ForEach(i =>
                {
                    if (i.Node is not null)
                    {
                        var character = new Character()
                        {
                            Id = i.Node.Id,
                            Name = i.Node?.Name?.UserPreferred,
                            Image = i.Node?.Image?.Medium,
                            Banner = media.Banner ?? media.Cover,
                            Role = i.Role
                        };

                        media.Characters.Add(character);
                    }
                });
            }

            if (fetchedMedia.Relations is not null)
            {
                media.Relations = new();

                fetchedMedia.Relations.Edges?.ForEach(mediaEdge =>
                {
                    var m = new Media(mediaEdge);
                    media.Relations.Add(m);

                    if (m.Relation == "SEQUEL")
                    {
                        media.Sequel = (media.Sequel?.Popularity ?? 0) < (m.Popularity ?? 0) ? m : media.Sequel;
                    }
                    else if (m.Relation == "PREQUEL")
                    {
                        media.Prequel = (media.Prequel?.Popularity ?? 0) < (m.Popularity ?? 0) ? m : media.Prequel;
                    }
                });

                media.Relations = media.Relations.OrderByDescending(x => x.Popularity).ToList();
                media.Relations = media.Relations.OrderByDescending(x => x.StartDate?.Year).ToList();
                media.Relations = media.Relations.OrderByDescending(x => x.Relation).ToList();
            }

            if (fetchedMedia.Recommendations is not null)
            {
                media.Recommendations = new();

                fetchedMedia.Recommendations.Nodes?.ForEach(i =>
                {
                    if (i.MediaRecommendation is not null)
                    {
                        media.Recommendations.Add(new Media(i.MediaRecommendation));
                    }
                });
            }

            if (fetchedMedia.MediaListEntry is not null)
            {
                media.UserProgress = fetchedMedia.MediaListEntry.Progress;
                media.IsListPrivate = fetchedMedia.MediaListEntry.IsPrivate ?? false;
                media.UserListId = fetchedMedia.MediaListEntry.Id;
                media.UserScore = (int?)fetchedMedia.MediaListEntry.Score ?? 0;
                media.UserStatus = fetchedMedia.MediaListEntry.Status?.ToString();
                //media.inCustomListsOf = fetchedMedia.MediaListEntry.Progress;
            }
            else
            {
            }

            if (media.Anime is not null)
            {
                var firstStudio = fetchedMedia.Studios?.Nodes?.FirstOrDefault();
                if (firstStudio is not null)
                {
                    media.MainStudio = new Studio()
                    {
                        Id = firstStudio.Id,
                        Name = firstStudio.Name ?? "N/A"
                    };
                }
            }

            return media;
        }

        return null;
    }

    public async Task<SearchResults?> SearchAsync(
        string type,
        int? page = null,
        int? perPage = null,
        string? search = null,
        string? sort = null,
        List<string>? genres = null,
        List<string>? tags = null,
        string? format = null,
        bool isAdult = false,
        bool? onList = null,
        int? id = null,
        bool hd = false)
    {
        var query = @"query ($page: Int = 1, $id: Int, $type: MediaType, $isAdult: Boolean = false, $search: String, $format: [MediaFormat], $status: MediaStatus, $countryOfOrigin: CountryCode, $source: MediaSource, $season: MediaSeason, $seasonYear: Int, $year: String, $onList: Boolean, $yearLesser: FuzzyDateInt, $yearGreater: FuzzyDateInt, $episodeLesser: Int, $episodeGreater: Int, $durationLesser: Int, $durationGreater: Int, $chapterLesser: Int, $chapterGreater: Int, $volumeLesser: Int, $volumeGreater: Int, $licensedBy: [String], $isLicensed: Boolean, $genres: [String], $excludedGenres: [String], $tags: [String], $excludedTags: [String], $minimumTagRank: Int, $sort: [MediaSort] = [POPULARITY_DESC, SCORE_DESC]) {
              Page(page: $page, perPage: " + $"{perPage ?? 50}" + @") {
                pageInfo {
                  total
                  perPage
                  currentPage
                  lastPage
                  hasNextPage
                }
                media(id: $id, type: $type, season: $season, format_in: $format, status: $status, countryOfOrigin: $countryOfOrigin, source: $source, search: $search, onList: $onList, seasonYear: $seasonYear, startDate_like: $year, startDate_lesser: $yearLesser, startDate_greater: $yearGreater, episodes_lesser: $episodeLesser, episodes_greater: $episodeGreater, duration_lesser: $durationLesser, duration_greater: $durationGreater, chapters_lesser: $chapterLesser, chapters_greater: $chapterGreater, volumes_lesser: $volumeLesser, volumes_greater: $volumeGreater, licensedBy_in: $licensedBy, isLicensed: $isLicensed, genre_in: $genres, genre_not_in: $excludedGenres, tag_in: $tags, tag_not_in: $excludedTags, minimumTagRank: $minimumTagRank, sort: $sort, isAdult: $isAdult) {
                  id
                  idMal
                  isAdult
                  status
                  chapters
                  episodes
                  nextAiringEpisode {
                    episode
                  }
                  type
                  genres
                  meanScore
                  isFavourite
                  bannerImage
                  coverImage {
                    large
                    extraLarge
                  }
                  title {
                    english
                    romaji
                    userPreferred
                  }
                  mediaListEntry {
                    progress
                    private
                    score(format: POINT_100)
                    status
                  }
                }
              }
            }".Replace("\n", " ").Replace(@"""  """, "");

        /*query = @"query ($id: Int) { # Define which variables will be used in the query (id)
              Media (id: $id, type: ANIME) { # Insert our variables into the query arguments (id) (type: ANIME is hard-coded in the query)
                id
                title {
                  romaji
                  english
                  native
                }
              }
            }";*/

        //var variables = @"{""id"": 1}";
        //variables = @"{""id"": 1, ""perPage"": 50}";

        var variables = $@"""type"": ""{type}"", ""isAdult"": {isAdult.ToString()!.ToLower()}";
        variables += onList is not null ? @$",""onList"":{onList.ToString()!.ToLower()}" : "";
        variables += page is not null ? @$",""page"":{page}" : "";
        variables += id is not null ? @$",""id"":{id}" : "";
        variables += search is not null ? @$",""search"":""{search}""" : "";
        variables += format is not null ? @$",""format"":{format}" : "";
        variables += genres is not null && genres!.Count > 0 ? @$",""genres"":{genres[0]}" : "";
        variables += tags is not null && tags!.Count > 0 ? @$",""tags"":{tags[0]}" : "";

        variables = "{" + variables.Replace("\n", " ").Replace(@"""  """, "") + "}";

        var response = (await ExecuteQueryAsync<Query.Page>(query, variables))?.Data?.Page;
        var responseArray = new List<Media>();

        if (response is null || response.Media is null)
            return null;

        response.Media.ForEach(i =>
        {
            var userStatus = i.MediaListEntry?.Status.ToString();
            var genresArr = new List<string>();
            if (i.Genres is not null)
            {
                i.Genres.AddRange(i.Genres);
            }

            var media = new Media(i);
            if (!hd)
                media.Cover = i.CoverImage?.Large;
            media.Relation = onList == true ? userStatus : null;
            media.Genres = genresArr;

            responseArray.Add(media);
        });

        var pageInfo = response?.PageInfo;

        return new SearchResults()
        {
            Type = type,
            PerPage = perPage,
            Search = search,
            Sort = sort,
            IsAdult = isAdult,
            OnList = onList,
            Genres = genres,
            Tags = tags,
            Format = format,
            Results = responseArray,
            Page = pageInfo?.CurrentPage ?? 0,
            HasNextPage = pageInfo?.HasNextPage == true,
        };
    }

    public async ValueTask<List<Media>?> GetRecentlyUpdatedAsync(
        bool smaller = true,
        long greater = 0,
        long? lesser = null)
    {
        lesser ??= DateTime.UtcNow.ToUnixTimeMilliseconds() / 1000 - 10000;

        async Task<Page?> Execute(int page = 1)
        {
            var query = @"{
                Page(page:${page},perPage:50) {
                    pageInfo {
                        hasNextPage
                        total
                    }
                    airingSchedules(
                        airingAt_greater: ${airingAt_greater}
                        airingAt_lesser: ${airingAt_lesser}
                        sort:TIME_DESC
                    ) {
                        media {
                            id
                            idMal
                            status
                            chapters
                            episodes
                            nextAiringEpisode { episode }
                            isAdult
                            type
                            meanScore
                            isFavourite
                            bannerImage
                            countryOfOrigin
                            coverImage { large }
                            title {
                                english
                                romaji
                                userPreferred
                            }
                            mediaListEntry {
                                progress
                                private
                                score(format: POINT_100)
                                status
                            }
                        }
                    }
                }
            }"
            .Replace("${page}", $"{page}")
            .Replace("${airingAt_lesser}", $"{lesser}")
            .Replace("${airingAt_greater}", $"{greater}")
            .Replace("\n", " ");

            return (await ExecuteQueryAsync<Query.Page>(query))?.Data?.Page;
        }

        if (smaller)
        {
            var response = (await Execute())?.AiringSchedules;

            if (response is null)
                return null;

            var responseArray = new List<Media>();
            var idArr = new List<int>();

            response.ForEach(x =>
            {
                if (x.Media is not null && !idArr.Contains(x.Media.Id))
                {
                    if (!ListOnly && x.Media.CountryOfOrigin == "JP" &&
                        (x.Media.IsAdult == IsAdult || ListOnly && x.Media.MediaListEntry is not null))
                    {
                        idArr.Add(x.Media.Id);
                        responseArray.Add(new Media(x.Media));
                    }
                }
            });

            return responseArray;
        }
        else
        {
            var i = 1;
            var list = new List<Media>();
            Page? res = null;

            async Task Next()
            {
                res = await Execute(i);

                res?.AiringSchedules?.ForEach(j =>
                {
                    if (j.Media is null)
                        return;

                    if (j.Media.CountryOfOrigin == "JP" &&
                        j.Media.IsAdult == IsAdult)
                    {
                        list!.Add(new Media(j.Media)
                        {
                            Relation = $"{j.Episode},{j.AiringAt}"
                        });
                    }
                });
            }

            await Next();

            while (res?.PageInfo?.HasNextPage == true)
            {
                await Next();
                i++;
            }

            list.Reverse();

            return list;
        }
    }

    public async Task<List<Media>?> GetTrendingAnimeAsync(
        int page = 1,
        int perPage = 50,
        string type = "ANIME")
    {
        var query = AnilistQueries.Trending(page, perPage, type);

        var response = (await ExecuteQueryAsync<Query.Page>(query))?.Data?.Page;

        if (response is null)
            return null;

        var responseArray = new List<Media>();

        response.Media?.ForEach(x =>
        {
            if (x is not null)
            {
                responseArray.Add(new Media(x));
            }
        });

        return responseArray;
        //return response.Media;
    }

    public async Task<Character> GetCharacterDetailsAsync(Character character)
    {
        var query = @"{
          Character(id: ${character.id}) {
            id
            age
            gender
            description
            dateOfBirth {
              year
              month
              day
            }
            media(page: 0,sort:[POPULARITY_DESC,SCORE_DESC]) {
              pageInfo {
                total
                perPage
                currentPage
                lastPage
                hasNextPage
              }
              edges {
                id
                characterRole
                node {
                  id
                  idMal
                  isAdult
                  status
                  chapters
                  episodes
                  nextAiringEpisode { episode }
                  type
                  meanScore
                  isFavourite
                  bannerImage
                  countryOfOrigin
                  coverImage { large }
                  title {
                      english
                      romaji
                      userPreferred
                  }
                  mediaListEntry {
                      progress
                      private
                      score(format: POINT_100)
                      status
                  }
                }
              }
            }
          }
        }"
        .Replace("${character.id}", $"{character.Id}")
        .Replace("\n", " ");

        var response = (await ExecuteQueryAsync<Query.Character>(query))?.Data?.Character;

        if (response is null)
            return character;

        character.Age = response.Age;
        character.Gender = response.Gender;
        character.Description = response.Description;
        character.DateOfBirth = response.DateOfBirth;

        return character;
    }

    public async Task<Studio> GetStudioDetailsAsync(Studio studio)
    {
        string Query(int page) => @"{
          Studio(id: ${studio.id}) {
            id
            media(page: $page,sort:START_DATE_DESC) {
              pageInfo{
                hasNextPage
              }
              edges {
                id
                node {
                  id
                  idMal
                  isAdult
                  status
                  chapters
                  episodes
                  nextAiringEpisode { episode }
                  type
                  meanScore
                  startDate{ year }
                  isFavourite
                  bannerImage
                  countryOfOrigin
                  coverImage { large }
                  title {
                      english
                      romaji
                      userPreferred
                  }
                  mediaListEntry {
                      progress
                      private
                      score(format: POINT_100)
                      status
                  }
                }
              }
            }
          }
        }"
        .Replace("${studio.id}", $"{studio.Id}")
        .Replace("$page", $"{page}")
        .Replace("\n", " ");

        var hasNextPage = true;
        var yearMedia = new Dictionary<string, List<Media>>();
        var page = 0;

        while (hasNextPage)
        {
            page++;

            var response = (await ExecuteQueryAsync<Query.Studio>(Query(page)))?.Data?.Studio?.Media;

            hasNextPage = response?.PageInfo?.HasNextPage == true;

            response?.Edges?.ForEach(edge =>
            {
                var year = edge.Node?.StartDate?.Year?.ToString() ?? "TBA";
                var title = edge.Node?.Status != MediaStatus.Cancelled ? year : edge.Node?.Status.ToString();

                if (title is null)
                    return;

                if (!yearMedia.ContainsKey(title))
                    yearMedia[title] = new List<Media>();

                yearMedia[title]?.Add(new Media(edge));
            });
        }

        //if (yearMedia.ContainsKey("CANCELLED"))
        //{
        //
        //}

        studio.YearMedia = yearMedia;

        return studio;
    }
}