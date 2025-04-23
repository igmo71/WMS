using System;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Descriptors.Documents
{
    internal class OrderOutDescriptor : IDocumentDescriptor
    {
        IEntityRepository IDocumentDescriptor.Repository => EntityRepositoryFactory.Get<OrderOut>();

        Document IDocumentDescriptor.CreateNew() => new OrderOut();

        ViewModelDescriptor IDocumentDescriptor.GetList() => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderOut)}",
            () => new DocumentListViewModel("Order Out", this));

        ViewModelDescriptor IDocumentDescriptor.GetMain(Document document) => new ViewModelDescriptor($"{nameof(OrderOutViewModel)}_{document.Id}",
            () => new OrderOutViewModel(document as OrderOut ?? throw new InvalidCastException()));

        string IDocumentDescriptor.GetUniqueKey(Document document) => $"{nameof(OrderInViewModel)}_{document.Id}";
    }
}
