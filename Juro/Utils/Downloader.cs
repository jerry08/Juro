using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Juro.Utils.Extensions;

namespace Juro.Utils;

public class Downloader
{
    public readonly HttpClient Client;

    public Downloader(HttpClient httpClient)
    {
        Client = httpClient;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Downloader"/>.
    /// </summary>
    public Downloader() : this(Http.Client)
    {
    }

    public async Task DownloadAsync(
        string url,
        Dictionary<string, string> headers,
        string filePath,
        IProgress<double>? progress = null,
        bool append = false,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        for (int j = 0; j < headers.Count; j++)
            request.Headers.TryAddWithoutValidation(headers.ElementAt(j).Key, headers.ElementAt(j).Value);

        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add(
                "User-Agent",
                Http.ChromeUserAgent()
            );
        }

        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode})." +
                Environment.NewLine +
                "Request:" +
            Environment.NewLine +
                request
            );
        }

        long totalLength = progress is not null ?
            await Client.GetFileSizeAsync(url,
                headers, cancellationToken) : 0;

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        //var file = File.Create(filePath);
        var file = new FileStream(filePath, FileMode.OpenOrCreate);

        if (append)
            file.Seek(0, SeekOrigin.End);

        try
        {
            await stream.CopyToAsync(file, progress, totalLength,
                cancellationToken: cancellationToken);
        }
        finally
        {
            file?.Close();
            stream?.Close();
        }
    }
}