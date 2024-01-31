using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Juro.Core.Utils.Extensions;

internal static class HttpExtensions
{
    // https://github.com/justfoolingaround/animdl/blob/master/animdl/utils/http_client.py#L68C15-L68C28
    public static HttpClient BypassDdg(this HttpClient http)
    {
        http.DefaultRequestHeaders.Add("User-Agent", Http.ChromeUserAgent());
        http.DefaultRequestHeaders.Add("Cookie", "__ddg2_=YW5pbWRsX3NheXNfaGkNCg.;");
        return http;
    }

    public static bool GetAllowAutoRedirect(this HttpClient http)
    {
        var handlerField = http.GetType()
            .BaseType!.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(x => x.Name.Contains("handler"))!;
        var handlerVal = handlerField.GetValue(http)!;

        var allowAutoRedirectProp = handlerVal
            .GetType()
            .GetProperty("AllowAutoRedirect", BindingFlags.Public | BindingFlags.Instance)!;
        var allowAutoRedirectPropVal = allowAutoRedirectProp.GetValue(handlerVal)!;

        return bool.Parse(allowAutoRedirectPropVal.ToString()!);
    }

    public static void SetAllowAutoRedirect(this HttpClient http, bool value)
    {
        var handlerField = http.GetType()
            .BaseType!.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(x => x.Name.Contains("handler"))!;
        var handlerVal = handlerField.GetValue(http)!;

        var allowAutoRedirectProp = handlerVal
            .GetType()
            .GetProperty("AllowAutoRedirect", BindingFlags.Public | BindingFlags.Instance)!;
        allowAutoRedirectProp.SetValue(handlerVal, value);
    }

    public static async ValueTask<HttpResponseMessage> HeadAsync(
        this HttpClient http,
        string requestUri,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
        return await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
    }

    public static async ValueTask<Stream> GetStreamAsync(
        this HttpClient http,
        string requestUri,
        long? from = null,
        long? to = null,
        bool ensureSuccess = true,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Range = new RangeHeaderValue(from, to);

        var response = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (ensureSuccess)
            response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public static async ValueTask<long?> TryGetContentLengthAsync(
        this HttpClient http,
        string requestUri,
        bool ensureSuccess = true,
        CancellationToken cancellationToken = default
    )
    {
        var response = await http.HeadAsync(requestUri, cancellationToken);

        if (ensureSuccess)
            response.EnsureSuccessStatusCode();

        return response.Content.Headers.ContentLength;
    }

    public static async ValueTask<string> GetAsync(
        this HttpClient http,
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> PostAsync(
        this HttpClient http,
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> PostAsync(
        this HttpClient http,
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        for (var j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(
                headers.ElementAt(j).Key!,
                headers.ElementAt(j).Value
            );

        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> PostAsync(
        this HttpClient http,
        string url,
        Dictionary<string, string> headers,
        HttpContent content,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        for (var j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(
                headers.ElementAt(j).Key!,
                headers.ElementAt(j).Value
            );

        request.Content = content;

        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> ExecuteAsync(
        this HttpClient http,
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> ExecuteAsync(
        this HttpClient http,
        string url,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        for (var j = 0; j < headers.Count; j++)
        {
            var header = headers.ElementAt(j);
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return await http.ExecuteAsync(request, cancellationToken);
    }

    public static async ValueTask<string> ExecuteAsync(
        this HttpClient http,
        HttpRequestMessage request,
        CancellationToken cancellationToken = default
    )
    {
        // User-agent
        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add("User-Agent", Http.ChromeUserAgent());
        }

        // Set required cookies
        //request.Headers.Add("Cookie", "CONSENT=YES+cb; YSC=DwKYllHNwuw");

        //Removed "using" to fix android.os.NetworkOnMainThreadException
        var response = await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
            return string.Empty;

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode})."
                    + Environment.NewLine
                    + "Request:"
                    + Environment.NewLine
                    + request
            );
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
