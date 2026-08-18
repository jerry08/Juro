using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Juro.Core.Models.Videos;
using Juro.Providers.Anime;
using Xunit;

namespace Juro.Tests.Specs.Anime;

public class AnikotoSourceSpecs
{
    [Fact]
    public async Task I_prefer_current_sources_and_preserve_the_cdn_selector()
    {
        // Arrange
        const string embedUrl = "https://megaplay.buzz/stream/s-2/14068/sub?s=tcdn";
        const string currentUrl =
            "https://megaplay.buzz/stream/getSourcesNew?id=135790&id=135790&type=sub&type=sub&s=tcdn";
        const string videoUrl = "https://ncdn.watching.onl/anime/bleach-276/master.m3u8";
        const string variantUrl = "https://ncdn.watching.onl/anime/bleach-276/index.m3u8";
        const string segmentUrl = "https://ncdn.watching.onl/anime/bleach-276/segment.ts";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"135790\"></div>"),
                currentUrl => JsonResponse($$"""{"sources":[{"file":"{{videoUrl}}"}]}"""),
                videoUrl => MasterPlaylistResponse(),
                variantUrl => MediaPlaylistResponse(),
                segmentUrl => TransportStreamResponse(),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var provider = new Anikoto(() => http);

        // Act
        var videos = await provider.GetVideosAsync(
            new VideoServer("Sub - HD-1", embedUrl),
            TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().ContainSingle().Which.VideoUrl.Should().Be(videoUrl);
        handler.RequestUrls.Should().Equal(embedUrl, currentUrl, videoUrl, variantUrl, segmentUrl);
    }

    [Fact]
    public async Task I_fall_back_to_legacy_sources_when_current_sources_are_empty()
    {
        // Arrange
        const string embedUrl = "https://megaplay.buzz/stream/s-2/14068/dub?s=bcdn";
        const string currentUrl =
            "https://megaplay.buzz/stream/getSourcesNew?id=135972&id=135972&type=dub&type=dub&s=bcdn";
        const string legacyUrl =
            "https://megaplay.buzz/stream/getSources?id=135972&id=135972&s=bcdn";
        const string videoUrl = "https://legacy.example/bleach-276/master.m3u8";
        const string variantUrl = "https://legacy.example/bleach-276/index.m3u8";
        const string segmentUrl = "https://legacy.example/bleach-276/segment.ts";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"135972\"></div>"),
                currentUrl => JsonResponse("{\"sources\":[]}"),
                legacyUrl => JsonResponse($$"""{"sources":[{"file":"{{videoUrl}}"}]}"""),
                videoUrl => MasterPlaylistResponse(),
                variantUrl => MediaPlaylistResponse(),
                segmentUrl => TransportStreamResponse(),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var provider = new Anikoto(() => http);

        // Act
        var videos = await provider.GetVideosAsync(
            new VideoServer("Dub - HD-2", embedUrl),
            TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().ContainSingle().Which.VideoUrl.Should().Be(videoUrl);
        handler
            .RequestUrls.Should()
            .Equal(embedUrl, currentUrl, legacyUrl, videoUrl, variantUrl, segmentUrl);
    }

    [Fact]
    public async Task I_fall_back_when_the_current_manifest_has_a_dead_media_segment()
    {
        // Arrange
        const string embedUrl = "https://megaplay.buzz/stream/s-2/14074/sub";
        const string currentApiUrl =
            "https://megaplay.buzz/stream/getSourcesNew?id=136098&id=136098&type=sub&type=sub";
        const string legacyApiUrl = "https://megaplay.buzz/stream/getSources?id=136098&id=136098";
        const string currentVideoUrl = "https://ncdn.example/bleach-282/master.m3u8";
        const string currentVariantUrl = "https://ncdn.example/bleach-282/index.m3u8";
        const string deadSegmentUrl = "https://dead.example/bleach-282/segment.jpg";
        const string legacyVideoUrl = "https://cdn.example/bleach-282/master.m3u8";
        const string legacyVariantUrl = "https://cdn.example/bleach-282/index.m3u8";
        const string healthySegmentUrl = "https://media.example/bleach-282/segment.jpg";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"136098\"></div>"),
                currentApiUrl => JsonResponse(
                    $"{{\"sources\":{{\"file\":\"{currentVideoUrl}\"}}}}"
                ),
                currentVideoUrl => MasterPlaylistResponse(),
                currentVariantUrl => MediaPlaylistResponse(deadSegmentUrl),
                deadSegmentUrl => new HttpResponseMessage(HttpStatusCode.NotFound),
                legacyApiUrl => JsonResponse($"{{\"sources\":{{\"file\":\"{legacyVideoUrl}\"}}}}"),
                legacyVideoUrl => MasterPlaylistResponse(),
                legacyVariantUrl => MediaPlaylistResponse(healthySegmentUrl),
                healthySegmentUrl => TransportStreamResponse("image/jpeg"),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var provider = new Anikoto(() => http);

        // Act
        var videos = await provider.GetVideosAsync(
            new VideoServer("Sub - Vidstream-2", embedUrl),
            TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().ContainSingle().Which.VideoUrl.Should().Be(legacyVideoUrl);
        handler
            .RequestUrls.Should()
            .Equal(
                embedUrl,
                currentApiUrl,
                currentVideoUrl,
                currentVariantUrl,
                deadSegmentUrl,
                legacyApiUrl,
                legacyVideoUrl,
                legacyVariantUrl,
                healthySegmentUrl
            );
    }

    [Fact]
    public async Task I_fall_back_when_the_current_source_probe_times_out()
    {
        // Arrange
        const string embedUrl = "https://megaplay.buzz/stream/s-2/14074/sub";
        const string currentApiUrl =
            "https://megaplay.buzz/stream/getSourcesNew?id=136098&id=136098&type=sub&type=sub";
        const string legacyApiUrl = "https://megaplay.buzz/stream/getSources?id=136098&id=136098";
        const string currentVideoUrl = "https://slow.example/bleach-282/master.m3u8";
        const string legacyVideoUrl = "https://cdn.example/bleach-282/master.m3u8";
        const string legacyVariantUrl = "https://cdn.example/bleach-282/index.m3u8";
        const string segmentUrl = "https://cdn.example/bleach-282/segment.ts";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"136098\"></div>"),
                currentApiUrl => JsonResponse(
                    $"{{\"sources\":{{\"file\":\"{currentVideoUrl}\"}}}}"
                ),
                currentVideoUrl => throw new TaskCanceledException("Simulated source timeout."),
                legacyApiUrl => JsonResponse($"{{\"sources\":{{\"file\":\"{legacyVideoUrl}\"}}}}"),
                legacyVideoUrl => MasterPlaylistResponse(),
                legacyVariantUrl => MediaPlaylistResponse(),
                segmentUrl => TransportStreamResponse(),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var provider = new Anikoto(() => http);

        // Act
        var videos = await provider.GetVideosAsync(
            new VideoServer("Sub - Vidstream-2", embedUrl),
            TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().ContainSingle().Which.VideoUrl.Should().Be(legacyVideoUrl);
        handler
            .RequestUrls.Should()
            .Equal(
                embedUrl,
                currentApiUrl,
                currentVideoUrl,
                legacyApiUrl,
                legacyVideoUrl,
                legacyVariantUrl,
                segmentUrl
            );
    }

    [Fact]
    public async Task I_reject_an_image_decoy_and_retry_without_the_cdn_selector()
    {
        // Arrange
        const string embedUrl = "https://megaplay.buzz/stream/s-2/14074/sub?s=tcdn";
        const string currentApiUrl =
            "https://megaplay.buzz/stream/getSourcesNew?id=136098&id=136098&type=sub&type=sub&s=tcdn";
        const string selectedLegacyApiUrl =
            "https://megaplay.buzz/stream/getSources?id=136098&id=136098&s=tcdn";
        const string defaultLegacyApiUrl =
            "https://megaplay.buzz/stream/getSources?id=136098&id=136098";
        const string currentVideoUrl = "https://ncdn.example/bleach-282/master.m3u8";
        const string currentVariantUrl = "https://ncdn.example/bleach-282/index.m3u8";
        const string deadSegmentUrl = "https://dead.example/bleach-282/segment.jpg";
        const string decoyVideoUrl = "https://decoy.example/bleach-282/master.m3u8";
        const string decoyVariantUrl = "https://decoy.example/bleach-282/index.m3u8";
        const string decoySegmentUrl = "https://images.example/not-video.png";
        const string healthyVideoUrl = "https://cdn.example/bleach-282/master.m3u8";
        const string healthyVariantUrl = "https://cdn.example/bleach-282/index.m3u8";
        const string healthySegmentUrl = "https://media.example/bleach-282/segment.jpg";
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"136098\"></div>"),
                currentApiUrl => JsonResponse(
                    $"{{\"sources\":{{\"file\":\"{currentVideoUrl}\"}}}}"
                ),
                currentVideoUrl => MasterPlaylistResponse(),
                currentVariantUrl => MediaPlaylistResponse(deadSegmentUrl),
                deadSegmentUrl => new HttpResponseMessage(HttpStatusCode.NotFound),
                selectedLegacyApiUrl => JsonResponse(
                    $"{{\"sources\":{{\"file\":\"{decoyVideoUrl}\"}}}}"
                ),
                decoyVideoUrl => MasterPlaylistResponse(),
                decoyVariantUrl => MediaPlaylistResponse(decoySegmentUrl),
                decoySegmentUrl => PngResponse(),
                defaultLegacyApiUrl => JsonResponse(
                    $"{{\"sources\":{{\"file\":\"{healthyVideoUrl}\"}}}}"
                ),
                healthyVideoUrl => MasterPlaylistResponse(),
                healthyVariantUrl => MediaPlaylistResponse(healthySegmentUrl),
                healthySegmentUrl => TransportStreamResponse("image/jpeg"),
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.RequestUri.AbsoluteUri}"
                ),
            }
        );
        using var http = new HttpClient(handler);
        var provider = new Anikoto(() => http);

        // Act
        var videos = await provider.GetVideosAsync(
            new VideoServer("Sub - HD-1", embedUrl),
            TestContext.Current.CancellationToken
        );

        // Assert
        videos.Should().ContainSingle().Which.VideoUrl.Should().Be(healthyVideoUrl);
        handler
            .RequestUrls.Should()
            .ContainInOrder(
                currentApiUrl,
                deadSegmentUrl,
                selectedLegacyApiUrl,
                decoySegmentUrl,
                defaultLegacyApiUrl,
                healthySegmentUrl
            );
    }

    private static HttpResponseMessage HtmlResponse(string content) =>
        Response(content, "text/html");

    private static HttpResponseMessage JsonResponse(string content) =>
        Response(content, "application/json");

    private static HttpResponseMessage MasterPlaylistResponse() =>
        Response(
            "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=1000000\nindex.m3u8\n",
            "application/vnd.apple.mpegurl"
        );

    private static HttpResponseMessage MediaPlaylistResponse(string url = "segment.ts") =>
        Response(
            $"#EXTM3U\n#EXT-X-TARGETDURATION:10\n#EXTINF:10,\n{url}\n#EXT-X-ENDLIST\n",
            "application/vnd.apple.mpegurl"
        );

    private static HttpResponseMessage TransportStreamResponse(string mediaType = "video/mp2t")
    {
        var bytes = new byte[564];
        bytes[0] = 0x47;
        bytes[188] = 0x47;
        bytes[376] = 0x47;
        return new HttpResponseMessage(HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers =
                {
                    ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType),
                },
            },
        };
    }

    private static HttpResponseMessage PngResponse() =>
        new(HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        };

    private static HttpResponseMessage Response(string content, string mediaType) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, mediaType) };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory
    ) : HttpMessageHandler
    {
        public List<string> RequestUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUrls.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(responseFactory(request));
        }
    }
}
