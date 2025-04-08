using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Services
{
    internal static class HTTPService
    {
        private readonly static Uri _host = new Uri("https://whs.verum.link");
        private readonly static HttpClient _httpClient;

        private readonly static Dictionary<string, string> _urls = new Dictionary<string, string>
        {
            { nameof(OrderIn), "/api/orders-in" },
            { nameof(Product), "/api/products" }
        };

        static HTTPService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = _host;
        }

        internal static List<T> GetList<T>() where T : EntityBase
        {
            string? url = _urls.GetValueOrDefault(typeof(T).Name);
            if (url == null)
                throw new NotSupportedException(typeof(T).Name);

            Task<List<T>> task = _httpClient.GetFromJsonAsync<List<T>>(url);
            task.Wait();

            return task.Result;
        }

        internal static T GetObject<T>(Guid id) where T : EntityBase
        {
            string? url = _urls.GetValueOrDefault(typeof(T).Name);
            if (url == null)
                throw new NotSupportedException(typeof(T).Name);

            Task<T> task = _httpClient.GetFromJsonAsync<T>($"{url}/{id}");
            task.Wait();

            return task.Result;
        }
    }
}
