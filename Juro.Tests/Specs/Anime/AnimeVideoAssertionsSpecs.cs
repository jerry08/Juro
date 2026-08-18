using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Juro.Core.Models.Videos;
using Xunit;

namespace Juro.Tests.Specs.Anime;

public class AnimeVideoAssertionsSpecs
{
    [Fact]
    public async Task Hls_probe_follows_the_variant_and_reads_real_media_bytes()
    {
        // Arrange
        const string masterUrl = "https://video.example/show/master.m3u8";
        const string variantUrl = "https://video.example/show/1080p/index.m3u8";
        const string segmentUrl = "https://cdn.example/show/segment.jpg";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                masterUrl => TextResponse(
                    "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=1000000\n1080p/index.m3u8\n",
                    "application/vnd.apple.mpegurl"
                ),
                variantUrl => TextResponse(
                    $"#EXTM3U\n#EXT-X-TARGETDURATION:10\n#EXTINF:10,\n{segmentUrl}\n",
                    "application/vnd.apple.mpegurl"
                ),
                segmentUrl => BinaryResponse(TransportStreamBytes(), "image/jpeg"),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var source = new VideoSource
        {
            VideoUrl = masterUrl,
            Format = VideoType.M3u8,
            Headers = new Dictionary<string, string> { ["Referer"] = "https://player.example/" },
        };

        // Act
        var result = await AnimeVideoAssertions.ProbeAsync(
            source,
            http,
            TestContext.Current.CancellationToken
        );

        // Assert
        result.IsPlayable.Should().BeTrue(result.Reason);
        handler.RequestUrls.Should().Equal(masterUrl, variantUrl, segmentUrl);
        handler.SegmentHadRangeHeader.Should().BeTrue();
        handler.SegmentReferer.Should().Be("https://player.example/");
    }

    [Fact]
    public async Task Hls_probe_rejects_an_image_decoy()
    {
        // Arrange
        const string playlistUrl = "https://video.example/show/index.m3u8";
        const string segmentUrl = "https://cdn.example/not-video.png";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                playlistUrl => TextResponse(
                    $"#EXTM3U\n#EXT-X-TARGETDURATION:10\n#EXTINF:10,\n{segmentUrl}\n",
                    "application/vnd.apple.mpegurl"
                ),
                segmentUrl => BinaryResponse(
                    [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
                    "image/png"
                ),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var source = new VideoSource { VideoUrl = playlistUrl, Format = VideoType.M3u8 };

        // Act
        var result = await AnimeVideoAssertions.ProbeAsync(
            source,
            http,
            TestContext.Current.CancellationToken
        );

        // Assert
        result.IsPlayable.Should().BeFalse();
        result.Reason.Should().Contain("not recognized media");
    }

    [Fact]
    public async Task Container_probe_accepts_an_mp4_file_signature()
    {
        // Arrange
        const string videoUrl = "https://video.example/movie.mp4";
        var mp4 = new byte[32];
        mp4[3] = 0x20;
        Encoding.ASCII.GetBytes("ftypisom").CopyTo(mp4, 4);
        var handler = new RecordingHandler(_ => BinaryResponse(mp4, "video/mp4"));
        using var http = new HttpClient(handler);
        var source = new VideoSource { VideoUrl = videoUrl, Format = VideoType.Container };

        // Act
        var result = await AnimeVideoAssertions.ProbeAsync(
            source,
            http,
            TestContext.Current.CancellationToken
        );

        // Assert
        result.IsPlayable.Should().BeTrue(result.Reason);
    }

    private static HttpResponseMessage TextResponse(string value, string mediaType) =>
        new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, mediaType) };

    private static HttpResponseMessage BinaryResponse(byte[] value, string mediaType) =>
        new(HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent(value)
            {
                Headers =
                {
                    ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType),
                },
            },
        };

    private static byte[] TransportStreamBytes()
    {
        var bytes = new byte[564];
        bytes[0] = 0x47;
        bytes[188] = 0x47;
        bytes[376] = 0x47;
        return bytes;
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory
    ) : HttpMessageHandler
    {
        public List<string> RequestUrls { get; } = [];

        public bool SegmentHadRangeHeader { get; private set; }

        public string? SegmentReferer { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUrls.Add(request.RequestUri!.AbsoluteUri);
            if (request.RequestUri.AbsoluteUri.Contains("segment", StringComparison.Ordinal))
            {
                SegmentHadRangeHeader = request.Headers.Range is not null;
                SegmentReferer = request.Headers.Referrer?.AbsoluteUri;
            }

            return Task.FromResult(responseFactory(request));
        }
    }
}
