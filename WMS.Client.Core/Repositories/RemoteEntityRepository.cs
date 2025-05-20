using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WMS.Client.Core.Adapters;
using WMS.Client.Core.Descriptors;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Services;
using WMS.Shared.Models;

namespace WMS.Client.Core.Repositories
{
    internal class RemoteEntityRepository<TEntity> : IEntityRepository where TEntity : EntityBase
    {
        private readonly Type _type;
        private readonly object _lock = new();
        private readonly Dictionary<Guid, WeakReference<EntityAdapter>> _cache = new();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        Type IEntityRepository.Type => _type;

        public event EventHandler<EntityCreatedEventArgs>? EntityCreated;
        public event EventHandler<EntityDeletedEventArgs>? EntityDeleted;
        public event EventHandler<EntityUpdatedEventArgs>? EntityUpdated;

        public RemoteEntityRepository()
        {
            _type = typeof(TEntity);

            SignalRClientService service = AppHost.GetService<SignalRClientService>();
            service.Subscribe<TEntity>($"{_type.Name}Created", e => EntityCreated?.Invoke(this, new EntityCreatedEventArgs(e)));
            service.Subscribe<TEntity>($"{_type.Name}Updated", e => EntityUpdated?.Invoke(this, new EntityUpdatedEventArgs(e)));
            service.Subscribe<Guid>($"{_type.Name}Deleted", id => EntityDeleted?.Invoke(this, new EntityDeletedEventArgs(id)));
        }

        IEnumerable<EntityBase> IEntityRepository.GetList() => AppHost.GetService<HTTPClientService>().Client.GetFromJsonAsync<IEnumerable<TEntity>>($"/api/{_type.Name}").GetAwaiter().GetResult();

        EntityAdapter IEntityRepository.GetById(Guid id)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(id, out WeakReference<EntityAdapter>? reference) && reference.TryGetTarget(out EntityAdapter target))
                    return target;
            }

            EntityAdapter adapter = EntityDescriptorFactory.Get<TEntity>()
                .GetAdapter(AppHost.GetService<HTTPClientService>().Client.GetFromJsonAsync<TEntity>($"/api/{_type.Name}/{id}").GetAwaiter().GetResult());

            lock (_lock)
            {
                if (!_cache.ContainsKey(adapter.Id))
                    _cache.Add(adapter.Id, new WeakReference<EntityAdapter>(adapter));
            }

            return adapter;
        }

        Guid IEntityRepository.CreateOrUpdate(EntityAdapter adapter)
        {
            Guid id = adapter.Id;
            if (adapter.IsNew)
            {
                HttpResponseMessage responce = AppHost.GetService<HTTPClientService>().Client.PostAsJsonAsync($"/api/{_type.Name}", (TEntity)adapter.GetEntity()).GetAwaiter().GetResult();
                EntityBase entity = JsonSerializer.Deserialize<TEntity>(responce.Content.ReadAsStringAsync().GetAwaiter().GetResult(), _jsonOptions) ?? throw new InvalidCastException();
                id = entity.Id;
            }
            else
            {
                AppHost.GetService<HTTPClientService>().Client.PutAsJsonAsync($"/api/{_type.Name}/{adapter.Id}", (TEntity)adapter.GetEntity());
            }

            lock (_lock)
            {
                if (!_cache.ContainsKey(adapter.Id))
                    _cache.Add(adapter.Id, new WeakReference<EntityAdapter>(adapter));
            }

            return id;
        }

        void IEntityRepository.Delete(Guid id) => AppHost.GetService<HTTPClientService>().Client.DeleteAsync($"/api/{_type.Name}/{id}");
    }
}
