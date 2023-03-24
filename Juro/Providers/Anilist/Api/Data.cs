using System.Collections.Generic;
using Newtonsoft.Json;

namespace Juro.Providers.Anilist.Api;

public class Query
{
    public class Viewer
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("Viewer")]
            public Api.User? User { get; set; }
        }
    }

    public class Media
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("Media")]
            public Api.Media? Media { get; set; }
        }
    }

    public class Page
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("page")]
            public Api.Page? Page { get; set; }
        }
    }

    public class Character
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("Character")]
            public Api.Character? Character { get; set; }
        }
    }

    public class Studio
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("Studio")]
            public Api.Studio? Studio { get; set; }
        }
    }

    public class MediaListCollection
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("MediaListCollection")]
            public Api.MediaListCollection? MediaListCollection { get; set; }
        }
    }

    public class GenreCollection
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("GenreCollection")]
            public List<string>? GenreCollection { get; set; }
        }
    }

    public class MediaTagCollection
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("MediaTagCollection")]
            public List<MediaTag>? MediaTagCollection { get; set; }
        }
    }

    public class User
    {
        [JsonProperty("data")]
        public Data2? Data { get; set; }

        public class Data2
        {
            [JsonProperty("User")]
            public Api.User? User { get; set; }
        }
    }
}