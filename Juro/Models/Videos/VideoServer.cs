namespace Juro.Models
{
    /// <summary>
    /// A simple class containing name and link of the embed which shows the video present on the site.
    /// </summary>
    public class VideoServer
    {
        public string Name { get; set; } = default!;

        public FileUrl Embed { get; set; } = default!;

        /// <summary>
        /// Initializes an instance of <see cref="VideoServer"/>.
        /// </summary>
        public VideoServer()
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="VideoServer"/>.
        /// </summary>
        public VideoServer(string url)
        {
            Name = "Default Server";
            Embed = new FileUrl(url);
        }

        /// <summary>
        /// Initializes an instance of <see cref="VideoServer"/>.
        /// </summary>
        public VideoServer(string name, FileUrl embed)
        {
            Name = name;
            Embed = embed;
        }

        /// <summary>
        /// Initializes an instance of <see cref="VideoServer"/>.
        /// </summary>
        public VideoServer(string name, string url)
        {
            Name = name;
            Embed = new FileUrl(url);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}