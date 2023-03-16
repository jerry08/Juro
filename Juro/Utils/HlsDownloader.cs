using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNetTools.JGrabber;
using DotNetTools.JGrabber.Grabbed;

namespace Juro.Utils;

public class HlsDownloader : Downloader
{
    public HlsDownloader(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="HlsDownloader"/>.
    /// </summary>
    public HlsDownloader() : this(Http.Client)
    {
    }

    public async Task<List<GrabbedHlsStreamMetadata>> GetHlsStreamMetadatasAsync(
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        var services = GrabberServicesBuilder.New()
            .UseHttpClientProvider(() =>
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers.ElementAt(i);

                    if (!Client.DefaultRequestHeaders.Contains(header.Key))
                        Client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }

                return Client;
            })
            .Build();

        var grabber = GrabberBuilder.New()
            //.UseDefaultServices()
            .UseServices(services)
            .AddHls()
            .Build();

        var grabResult = await grabber.GrabAsync(new Uri(url), cancellationToken: cancellationToken);

        return grabResult.Resources<GrabbedHlsStreamMetadata>().ToList();
    }

    /// <summary>
    /// Downloads a hls/m3u8 video from a url. To prevent slight non synchronization
    /// with the audio/video, you can run the ffmpeg command:
    /// ffmpeg -i C:\path\video.ts -acodec copy -vcodec copy C:\path\video.mp4
    /// </summary>
    public async Task DownloadTsAsync(
        GrabbedHlsStream stream,
        Dictionary<string, string> headers,
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < stream.Segments.Count; i++)
        {
            var segment = stream.Segments[i];
            //Console.Write($"Downloading segment #{i + 1} {segment.Title}...");
            await DownloadAsync(segment.Uri.AbsoluteUri, headers, filePath, null, true, cancellationToken);
            //Console.WriteLine(" OK");

            progress?.Report(((double)i / (double)stream.Segments.Count * 100) / 100);
        }
    }

    /// <summary>
    /// Downloads a hls/m3u8 video from a url.
    /// </summary>
    public async Task DownloadAllTsThenMergeAsync(
        GrabbedHlsStream stream,
        Dictionary<string, string> headers,
        string filePath,
        IProgress<double>? progress = null,
        int maxParallelDownloads = 10,
        CancellationToken cancellationToken = default)
    {
        var tempFiles = new List<string>();
        try
        {
            using var downloadSemaphore = new ResizableSemaphore
            {
                MaxCount = maxParallelDownloads
            };

            var total = 0;

            var tasks = Enumerable.Range(0, stream.Segments.Count).Select(i =>
                Task.Run(async () =>
                {
                    using var access = await downloadSemaphore.AcquireAsync(cancellationToken);

                    var segment = stream.Segments[i];

                    var outputPath = Path.Combine(Path.GetTempPath(), DateTime.Now.Ticks.ToString()) + $"_{i}.tmp";
                    tempFiles.Add(outputPath);
                    await DownloadAsync(segment.Uri.AbsoluteUri, headers, outputPath, null, false, cancellationToken);

                    total++;

                    progress?.Report(((double)total / (double)stream.Segments.Count * 100) / 100);
                }));

            await Task.WhenAll(tasks);

            progress?.Report(1);

            tempFiles = tempFiles.OrderBy(x => Convert.ToInt32(Path.GetFileNameWithoutExtension(x)
                .Split('_').LastOrDefault())).ToList();

            await FileEx.CombineMultipleFilesIntoSingleFile(tempFiles, filePath);
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            //Console.WriteLine("Cleaned up temp files.");
        }
    }
}