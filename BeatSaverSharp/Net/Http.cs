using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp.Exceptions;
using BeatSaverSharp.Net;
using Newtonsoft.Json;

namespace BeatSaverSharp
{
    internal sealed class Http
    {
        internal static readonly JsonSerializer Serializer = new();

        internal HttpOptions Options { get; }
        internal HttpClient Client { get; }

        internal Http(HttpOptions? options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            Client = new HttpClient(handler)
            {
                BaseAddress = new Uri($"{BeatSaver.BaseURL}/api/"),
                Timeout = Options.Timeout,
            };

            string libVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string userAgent = $"{Options.ApplicationName}/{Options.Version} BeatSaverSharp/{libVersion}";

            foreach (var agent in Options.Agents)
            {
                if (agent.Name is null) throw new NullReferenceException($"{nameof(agent)}.{nameof(agent.Name)} must not be null!");
                if (agent.Version is null) throw new NullReferenceException($"{nameof(agent)}.{nameof(agent.Version)} must not be null!");

                userAgent += $" {agent.Name}/{agent.Version}";
            }

            Client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }

        internal async Task<HttpResponse> GetAsync(HttpRequest request)
        {
            var token = request.Token ?? CancellationToken.None;
            var msg = new HttpRequestMessage(HttpMethod.Get, request.Uri)
            {
                Version = Options.HttpVersion,
            };

            foreach (var header in request.Headers)
            {
                if (header.Key.ToLower() == "User-Agent".ToLower()) continue;
                msg.Headers.Add(header.Key, header.Value);
            }

            var resp = await Client
                .SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            if ((int)resp.StatusCode == 429)
            {
                var ex = new RateLimitExceededException(resp);
                if (Options.HandleRateLimits == false)
                {
                    throw ex;
                }

                RateLimitInfo info = ex.RateLimit;
                TimeSpan diff = info.Reset - DateTime.Now;

                int millis = (int)diff.TotalMilliseconds;
                if (millis > 0)
                {
                    await Task.Delay(millis).ConfigureAwait(false);
                }

                return await GetAsync(request).ConfigureAwait(false);
            }

            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            using MemoryStream ms = new MemoryStream();
            using Stream s = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);

            byte[] buffer = new byte[1 << 13];
            int bytesRead;

            long? contentLength = resp.Content.Headers.ContentLength;
            long totalRead = 0;
            request.Progress?.Report(0);

            while ((bytesRead = await s.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                if (token.IsCancellationRequested) throw new TaskCanceledException();
                if (contentLength is not null)
                {
                    double prog = (double)totalRead / (double)contentLength;
                    request.Progress?.Report(prog);
                }

                await ms.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                totalRead += bytesRead;
            }

            request.Progress?.Report(1);
            byte[] bytes = ms.ToArray();

            return new HttpResponse(resp, bytes);
        }
    }
}
