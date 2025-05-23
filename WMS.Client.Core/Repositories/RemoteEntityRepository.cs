﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
        }

        Type IEntityRepository.Type => _type;

        IEnumerable<EntityBase> IEntityRepository.GetList()
        {
            Task<IEnumerable<TEntity>> task = HTTPClientProvider.Instance.GetFromJsonAsync<IEnumerable<TEntity>>(_api);
            task.Wait();

            return task.Result;
        }

        EntityBase IEntityRepository.GetById(Guid id)
        {
            Task<TEntity> task = HTTPClientProvider.Instance.GetFromJsonAsync<TEntity>($"{_api}/{id}");
            task.Wait();

            return task.Result;
        }

        void IEntityRepository.Create(EntityBase entity)
        {
            Task<HttpResponseMessage> task = HTTPClientProvider.Instance.PostAsJsonAsync(_api, (TEntity)entity);
            task.Wait();

            EntityCreated?.Invoke(this, new EntityChangedEventArgs(entity));
        }

        void IEntityRepository.Delete(EntityBase entity)
        {
            Task<HttpResponseMessage> task = HTTPClientProvider.Instance.DeleteAsync($"{_api}/{entity.Id}");
            task.Wait();

            EntityDeleted?.Invoke(this, new EntityChangedEventArgs(entity));
        }

        void IEntityRepository.Update(EntityBase entity)
        {
            Task<HttpResponseMessage> task = HTTPClientProvider.Instance.PutAsJsonAsync($"{_api}/{entity.Id}", (TEntity)entity);
            task.Wait();

            EntityUpdated?.Invoke(this, new EntityChangedEventArgs(entity));
        }
    }
}
