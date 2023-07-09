using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Juro.Providers.Anilist.Api
{
    public class Query
    {
        public class Viewer
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("Viewer")] public Api.User? User { get; set; }
            }
        }

        public class Media
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("Media")] public Api.Media? Media { get; set; }
            }
        }

        public class Page
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("page")] public Api.Page? Page { get; set; }
            }
        }

        public class Character
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("Character")] public Api.Character? Character { get; set; }
            }
        }

        public class Studio
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("Studio")] public Api.Studio? Studio { get; set; }
            }
        }

        public class MediaListCollection
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("MediaListCollection")]
                public Api.MediaListCollection? MediaListCollection { get; set; }
            }
        }

        public class GenreCollection
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("GenreCollection")] public List<string>? GenreCollection { get; set; }
            }
        }

        public class MediaTagCollection
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("MediaTagCollection")]
                public List<MediaTag>? MediaTagCollection { get; set; }
            }
        }

        public class User
        {
            [JsonPropertyName("data")] public Data2? Data { get; set; }

            public class Data2
            {
                [JsonPropertyName("User")] public Api.User? User { get; set; }
            }
        }
    }
}