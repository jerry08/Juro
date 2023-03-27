using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Juro.Models.Videos;
using Juro.Utils;
using Juro.Utils.Extensions;
using Newtonsoft.Json.Linq;

namespace Juro.Extractors;

/// <summary>
/// Extractor for Gogoanime.
/// </summary>
public class GogoCDN : IVideoExtractor
{
    private readonly Func<HttpClient> _httpClientProvider;

    public string ServerName => "Gogo";

    public GogoCDN(Func<HttpClient> httpClientProvider)
    {
        _httpClientProvider = httpClientProvider;
    }

    public async Task<List<VideoSource>> ExtractAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var http = _httpClientProvider();

        var host = new Uri(url).Host;

        var list = new List<VideoSource>();

        var response = await http.ExecuteAsync(url, cancellationToken);

        if (url.Contains("streaming.php"))
        {
            var document = Html.Parse(response);

            //var script = doc.DocumentNode
            //    .SelectSingleNode("script[data-name='crypto'");

            //var tt = doc.DocumentNode.Descendants("script")
            //    .Where(x => x.Name == "script").ToList();

            var scripts = document.DocumentNode.Descendants()
                .Where(x => x.Name == "script").ToList();

            var cryptoScript = scripts.FirstOrDefault(x => x.Attributes["data-name"]?.Value == "episode")!;

            //var dataValue = scripts.Where(x => x.Attributes["data-name"]?.Value == "crypto")
            //  .FirstOrDefault().Attributes["data-value"].Value;

            var dataValue = cryptoScript.Attributes["data-value"].Value;

            var keys = KeysAndIv();

            var decrypted = CryptoHandler(dataValue, keys.Item1, keys.Item3, false).Replace("\t", "");
            var id = decrypted.FindBetween("", "&");
            var end = decrypted.SubstringAfter(id);

            var link = $"https://{host}/encrypt-ajax.php?id={CryptoHandler(id, keys.Item1, keys.Item3, true)}{end}&alias={id}";

            var encHtmlData = await http.ExecuteAsync(link,
                new Dictionary<string, string>()
                {
                    { "X-Requested-With", "XMLHttpRequest" },
                    //{ "Referer", host },
                }, cancellationToken);

            if (string.IsNullOrWhiteSpace(encHtmlData))
                return list;

            var jsonObj = JObject.Parse(encHtmlData);
            var jumbledJson = CryptoHandler(jsonObj["data"]!.ToString(), keys.Item2, keys.Item3, false);
            jumbledJson = jumbledJson.Replace(@"o""<P{#meme"":""", @"e"":[{""file"":""");

            var source = JObject.Parse(jumbledJson)["source"]!.ToString();
            var array = JArray.Parse(source);

            var sourceBk = JObject.Parse(jumbledJson)["source_bk"]!.ToString();
            var arrayBk = JArray.Parse(sourceBk);

            void AddSources(JArray array)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    var label = array[i]["label"]!.ToString();
                    var fileURL = array[i]["file"]!.ToString().Trim('"');
                    var type = array[i]["type"]?.ToString().ToLower();

                    if (type == "hls" || type == "auto")
                    {
                        list.Add(new()
                        {
                            Format = VideoType.M3u8,
                            VideoUrl = fileURL,
                            Resolution = "Multi Quality",
                            Headers = new()
                            {
                                { "Referer", url },
                            }
                        });
                    }
                    else
                    {
                        list.Add(new()
                        {
                            Format = VideoType.Container,
                            VideoUrl = fileURL,
                            Resolution = label,
                            Headers = new()
                            {
                                { "Referer", url },
                            }
                        });
                    }
                }
            }

            AddSources(array);
            AddSources(arrayBk);

            /*for (int i = 0; i < array.Count; i++)
            {
                var type = array[i]["type"]?.ToString().ToLower();

                if (type == "hls" || type == "auto")
                {
                    var fileURL = array[i]["file"]!.ToString().Trim('"');
                    var masterPlaylist = await _http.ExecuteAsync(fileURL, cancellationToken);
                    var masterSplit = masterPlaylist.Split(new string[] { "#EXT-X-STREAM-INF:" }, StringSplitOptions.None).ToList();
                    masterSplit.Remove(masterSplit[0]);

                    for (int j = 0; j < masterSplit.Count; j++)
                    {
                        var videoUrlSplit = fileURL.Split('/').ToList();
                        videoUrlSplit.RemoveAt(videoUrlSplit.Count - 1);
                        var itSplit = masterSplit[j].Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
                        itSplit.RemoveAll(x => string.IsNullOrWhiteSpace(x));

                        var video = itSplit[0].SubstringAfter("RESOLUTION=").SubstringBefore("x") + " p";
                        var videoUrl = string.Join("/", videoUrlSplit) + "/" + itSplit.LastOrDefault();

                        list.Add(new Video()
                        {
                            Format = VideoType.M3u8,
                            VideoUrl = videoUrl,
                            Resolution = video,
                            Headers = new()
                            {
                                { "Referer", url },
                            }
                        });
                    }
                }
                else
                {
                    var label = array[i]["label"]!.ToString();
                    var fileURL = array[i]["file"]!.ToString().Trim('"');

                    list.Add(new Video()
                    {
                        Format = VideoType.Container,
                        VideoUrl = fileURL,
                        Resolution = label,
                        Headers = new()
                        {
                            { "Referer", url },
                        }
                    });
                }
            }*/
        }
        else if (url.Contains("embedplus"))
        {
            var file = response.FindBetween("sources:[{file: '", "',");
            if (!string.IsNullOrWhiteSpace(file))
            {
                list.Add(new()
                {
                    Format = VideoType.M3u8,
                    VideoUrl = file,
                    Headers = new()
                    {
                        { "Referer", url },
                    }
                });
            }
        }

        return list;
    }

    private Tuple<string, string, string> KeysAndIv()
    {
        return new Tuple<string, string, string>
            ("37911490979715163134003223491201", "54674138327930866480207815084989", "3134003223491201");
    }

    private static string CryptoHandler(string dataValue, string key, string iv, bool encrypt = true)
    {
        //var key = Encoding.UTF8.GetBytes("63976882873559819639988080820907");
        //var iv = Encoding.UTF8.GetBytes("4770478969418267");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ivBytes = Encoding.UTF8.GetBytes(iv);

        var cryptoProvider = new RijndaelManaged();
        cryptoProvider.Mode = CipherMode.CBC;
        cryptoProvider.Padding = PaddingMode.PKCS7;

        if (encrypt)
        {
            // Convert from Base64 to binary
            var bytIn = Encoding.ASCII.GetBytes(dataValue);

            //var padding = new byte[] { 0x8, 0xe, 0x3, 0x8, 0x9, 0x3, 0x4, 0x9 };
            //bytIn = bytIn.Concat(padding).ToArray();

            // Create a MemoryStream
            var ms = new MemoryStream();

            // Create Crypto Stream that encrypts a stream
            var cs = new CryptoStream(ms,
                cryptoProvider.CreateEncryptor(keyBytes, ivBytes),
                CryptoStreamMode.Write);

            // Write content into MemoryStream
            cs.Write(bytIn, 0, bytIn.Length);
            cs.FlushFinalBlock();

            var bytOut = ms.ToArray();
            return Convert.ToBase64String(bytOut);
        }
        else
        {
            // Convert from Base64 to binary
            var bytIn = Convert.FromBase64String(dataValue);

            // Create a MemoryStream
            var ms = new MemoryStream(bytIn, 0, bytIn.Length);

            // Create a CryptoStream that decrypts the data
            var cs = new CryptoStream(ms,
                cryptoProvider.CreateDecryptor(keyBytes, ivBytes),
                CryptoStreamMode.Read);

            // Read the Crypto Stream
            var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}