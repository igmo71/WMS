using System;
using System.Net.Http;

namespace WMS.Client.Core.Services
{
    internal class HTTPClientService : IDisposable
    {
        private readonly HttpClient _httpClient;

        internal HttpClient Client => _httpClient;

        public HTTPClientService()
        {
            SocketsHttpHandler handler = new SocketsHttpHandler();
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri("http://igmo-pc:8220");
        }

        public void Dispose() => _httpClient.Dispose();
    }
}
