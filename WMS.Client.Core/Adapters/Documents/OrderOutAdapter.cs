using System;
using WMS.Shared.Models;

namespace WMS.Client.Core.Adapters.Documents
{
    internal class OrderOutAdapter : DocumentAdapter
    {
        public OrderOutAdapter(EntityBase entity) : base(entity)
        {
        }

        internal override EntityBase GetEntity() => throw new NotImplementedException();

        internal override void Save() => throw new NotImplementedException();
    }
}
