using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;
using WMS.Shared.Models.Catalogs;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Repositories
{
    internal class RemoteEntityRepository<TEntity> : IEntityRepository where TEntity : EntityBase
    {
        private readonly Type _type;
        private readonly string _api;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        Type IEntityRepository.Type => _type;

        public event EventHandler<EntityChangedEventArgs> EntityCreated;
        public event EventHandler<EntityChangedEventArgs> EntityDeleted;
        public event EventHandler<EntityChangedEventArgs> EntityUpdated;

        public RemoteEntityRepository()
        {
            _type = typeof(TEntity);

            Dictionary<Type, string> apis = new Dictionary<Type, string>
            {
                { typeof(OrderIn), "/api/orders-in" },
                { typeof(Product), "/api/products" }
            };
            _api = apis.GetValueOrDefault(_type) ?? throw new NotSupportedException();

            AppHost.GetService<KafkaConsumerService>().MessageConsumed += KafkaMessageConsumed;
        }

        private void KafkaMessageConsumed(object? sender, KafkaMessageConsumedEventArgs e)
        {
            if (!e.Topic.ToUpper().StartsWith(_type.Name.ToUpper()))
                return;

            TEntity entity = JsonSerializer.Deserialize<TEntity>(e.Message, _jsonOptions) ?? throw new InvalidCastException();

            string eventName = e.Topic.Substring(_type.Name.Length).ToUpper();
            switch (eventName)
            {
                case "CREATED":
                    EntityCreated?.Invoke(this, new EntityChangedEventArgs(entity));
                    break;
                case "UPDATED":
                    EntityUpdated?.Invoke(this, new EntityChangedEventArgs(entity));
                    break;
                case "DELETED":
                    EntityDeleted?.Invoke(this, new EntityChangedEventArgs(entity));
                    break;
            }
        }

        IEnumerable<EntityBase> IEntityRepository.GetList() => AppHost.GetService<HTTPClientService>().Client.GetFromJsonAsync<IEnumerable<TEntity>>(_api).GetAwaiter().GetResult();

        EntityBase IEntityRepository.GetById(Guid id) => AppHost.GetService<HTTPClientService>().Client.GetFromJsonAsync<TEntity>($"{_api}/{id}").GetAwaiter().GetResult();

        EntityBase IEntityRepository.Create(EntityBase entity)
        {
            HttpResponseMessage responce = AppHost.GetService<HTTPClientService>().Client.PostAsJsonAsync(_api, (TEntity)entity).GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<TEntity>(responce.Content.ReadAsStringAsync().GetAwaiter().GetResult(), _jsonOptions) ?? throw new InvalidCastException();
        }

        void IEntityRepository.Delete(EntityBase entity) => AppHost.GetService<HTTPClientService>().Client.DeleteAsync($"{_api}/{entity.Id}");

        void IEntityRepository.Update(EntityBase entity) => AppHost.GetService<HTTPClientService>().Client.PutAsJsonAsync($"{_api}/{entity.Id}", (TEntity)entity);
    }
}
