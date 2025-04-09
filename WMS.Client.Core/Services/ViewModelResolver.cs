using System;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Services
{
    internal static class ViewModelResolver
    {
        internal static ViewModelDescriptor GetMain(EntityBase entity)
        {
            return entity switch
            {
                OrderIn d => new ViewModelDescriptor($"{nameof(OrderInViewModel)}_{entity.Id}", () => new OrderInViewModel(d)),
                _ => throw new NotSupportedException()
            };
        }

        internal static ViewModelDescriptor GetList(Type type)
        {
            return type switch
            {
                Type t when t == typeof(OrderIn) => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderIn)}", 
                    () => new DocumentListViewModel(EntityRepositoryFactory.Get<OrderIn>()) { Name = "Order In" }),
                _ => throw new NotSupportedException()
            };
        }
    }

    internal readonly struct ViewModelDescriptor
    {
        public string UniqueKey { get; }
        public Func<ViewModelBase> Factory { get; }

        public ViewModelDescriptor(string uniqueKey, Func<ViewModelBase> factory)
        {
            UniqueKey = uniqueKey;
            Factory = factory;
        }
    }
}
