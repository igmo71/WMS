using System;
using WMS.Client.Core.Adapters;
using WMS.Client.Core.Adapters.Documents;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels.Documents;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Descriptors.Documents
{
    internal class OrderOutDescriptor : IEntityDescriptor
    {
        IEntityRepository IEntityDescriptor.Repository => EntityRepositoryFactory.Get<OrderOut>();

        EntityAdapter IEntityDescriptor.CreateNew() => throw new NotImplementedException();
        EntityAdapter IEntityDescriptor.GetAdapter(EntityBase entity) => throw new NotImplementedException();

        ViewModelDescriptor IEntityDescriptor.GetList() => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderOut)}",
            () => new DocumentListViewModel("Order Out", this));

        ViewModelDescriptor IEntityDescriptor.GetMain(EntityAdapter adapter) => new ViewModelDescriptor($"{nameof(OrderOutViewModel)}_{adapter.Id}",
            () => new OrderOutViewModel(adapter as OrderOutAdapter ?? throw new InvalidCastException()));

        string IEntityDescriptor.GetUniqueKey(EntityAdapter adapter) => $"{nameof(OrderInViewModel)}_{adapter.Id}";
    }
}
