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
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"135790\"></div>"),
                currentUrl => JsonResponse($$"""{"sources":[{"file":"{{videoUrl}}"}]}"""),
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
        handler.RequestUrls.Should().Equal(embedUrl, currentUrl);
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
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsoluteUri switch
            {
                embedUrl => HtmlResponse("<div data-id=\"135972\"></div>"),
                currentUrl => JsonResponse("{\"sources\":[]}"),
                legacyUrl => JsonResponse($$"""{"sources":[{"file":"{{videoUrl}}"}]}"""),
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
        handler.RequestUrls.Should().Equal(embedUrl, currentUrl, legacyUrl);
    }

    private static HttpResponseMessage HtmlResponse(string content) =>
        Response(content, "text/html");

    private static HttpResponseMessage JsonResponse(string content) =>
        Response(content, "application/json");

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
