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
    internal class OrderInDescriptor : IEntityDescriptor
    {
        IEntityRepository IEntityDescriptor.Repository => EntityRepositoryFactory.Get<OrderIn>();

        EntityAdapter IEntityDescriptor.CreateNew() => new OrderInAdapter(new OrderIn());

        EntityAdapter IEntityDescriptor.GetAdapter(EntityBase entity) => new OrderInAdapter(entity);

        ViewModelDescriptor IEntityDescriptor.GetList() => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderIn)}",
            () => new DocumentListViewModel("Order In", this));

        ViewModelDescriptor IEntityDescriptor.GetMain(EntityAdapter adapter) => new ViewModelDescriptor($"{nameof(OrderInViewModel)}_{adapter.Id}",
            () => new OrderInViewModel(adapter as OrderInAdapter ?? throw new InvalidCastException()));

        string IEntityDescriptor.GetUniqueKey(EntityAdapter adapter) => $"{nameof(OrderInViewModel)}_{adapter.Id}";

    }
}
