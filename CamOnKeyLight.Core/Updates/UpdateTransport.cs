namespace CamOnKeyLight.Core
{
    public sealed class RedirectGuardHandler : DelegatingHandler
    {
        public const int MaxRedirects = 5;

        public RedirectGuardHandler(HttpMessageHandler inner)
            : base(inner)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null || !UrlPolicy.IsAllowedTransport(request.RequestUri))
                throw new InvalidOperationException("The update address is not a Techboy/CamOnKeyLight GitHub address.");

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            int hops = 0;
            while (IsRedirect(response.StatusCode) && hops < MaxRedirects)
            {
                Uri? next = response.Headers.Location;
                if (next == null)
                    break;

                if (!next.IsAbsoluteUri)
                    next = new Uri(request.RequestUri!, next);

                if (!UrlPolicy.IsAllowedTransport(next))
                {
                    response.Dispose();
                    throw new InvalidOperationException("The update download was redirected away from GitHub.");
                }

                HttpRequestMessage follow = CloneForRedirect(request, next);
                response.Dispose();
                response = await base.SendAsync(follow, cancellationToken);
                hops++;
            }

            if (IsRedirect(response.StatusCode))
            {
                response.Dispose();
                throw new InvalidOperationException("The update download redirected too many times.");
            }

            return response;
        }

        private static HttpRequestMessage CloneForRedirect(HttpRequestMessage from, Uri next)
        {
            HttpRequestMessage follow = new HttpRequestMessage(HttpMethod.Get, next);
            if (from.Headers.UserAgent.Count > 0)
                follow.Headers.UserAgent.ParseAdd(from.Headers.UserAgent.ToString());
            if (from.Headers.Accept.Count > 0)
                follow.Headers.Accept.ParseAdd(from.Headers.Accept.ToString());
            return follow;
        }

        private static bool IsRedirect(System.Net.HttpStatusCode status)
        {
            int code = (int)status;
            return code is 301 or 302 or 303 or 307 or 308;
        }
    }

    public static class UpdateClientFactory
    {
        public static HttpClient Create()
        {
            RedirectGuardHandler guard = new RedirectGuardHandler(new HttpClientHandler { AllowAutoRedirect = false });
            HttpClient client = new HttpClient(guard);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CamOnKeyLight");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
            return client;
        }
    }

    public static class LimitedDownloader
    {
        public static async Task<byte[]> ReadAsync(HttpClient client, Uri url, long maxBytes, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long declared && declared > maxBytes)
                throw new InvalidDataException("The download is larger than expected.");

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using MemoryStream buffer = new MemoryStream();
            byte[] chunk = new byte[81920];
            long total = 0;
            while (true)
            {
                int read = await stream.ReadAsync(chunk, cancellationToken);
                if (read == 0)
                    break;

                total += read;
                if (total > maxBytes)
                    throw new InvalidDataException("The download is larger than expected.");

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
    }
}
