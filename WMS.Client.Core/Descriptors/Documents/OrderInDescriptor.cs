using System;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Interfaces;
using WMS.Client.Core.Repositories;
using WMS.Client.Core.ViewModels.Documents;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Descriptors.Documents
{
    internal class OrderInDescriptor : IDocumentDescriptor
    {
        IEntityRepository IDocumentDescriptor.Repository => EntityRepositoryFactory.Get<OrderIn>();

        Document IDocumentDescriptor.CreateNew() => new OrderIn();

        ViewModelDescriptor IDocumentDescriptor.GetList() => new ViewModelDescriptor($"{nameof(DocumentListViewModel)}_{nameof(OrderIn)}",
            () => new DocumentListViewModel("Order In", this));

        ViewModelDescriptor IDocumentDescriptor.GetMain(Document document) => new ViewModelDescriptor($"{nameof(OrderInViewModel)}_{document.Id}",
            () => new OrderInViewModel(document as OrderIn ?? throw new InvalidCastException()));

        string IDocumentDescriptor.GetUniqueKey(Document document) => $"{nameof(OrderInViewModel)}_{document.Id}";
    }
}
