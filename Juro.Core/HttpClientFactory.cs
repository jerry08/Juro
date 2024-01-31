using System;
using System.Net.Http;

namespace Juro.Core;

internal class HttpClientFactory(Func<HttpClient> httpClientFunc) : IHttpClientFactory
{
    private readonly Func<HttpClient> _httpClientFunc = httpClientFunc;

    public HttpClientFactory()
        : this(() => new()) { }

    public HttpClient CreateClient() => _httpClientFunc();
}
