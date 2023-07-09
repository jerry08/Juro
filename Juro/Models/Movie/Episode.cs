namespace Juro.Models.Movie
{
    public class Episode
    {
        public string Id { get; set; } = default!;

        public string Title { get; set; } = default!;

        public int Number { get; set; } = default!;

        public int Season { get; set; } = default!;

        public string Url { get; set; } = default!;
    }
}