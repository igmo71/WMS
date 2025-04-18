using System;
using System.Net.Http;

namespace WMS.Client.Core.Services
{
    internal static class HTTPClientProvider
    {
        private static readonly HttpClient _httpClient;

        internal static HttpClient Instance => _httpClient;

        static HTTPClientProvider()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://vm-igmo-dev:8220");
        }
    }
}
