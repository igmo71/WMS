using System;
using WMS.Shared.Models;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.Adapters.Documents
{
    internal abstract class DocumentAdapter : EntityAdapter
    {
        private string _number;
        private DateTime _dateTime;

        internal string Number { get => LockAndGet(ref _number); set => SetAndNotify(ref _number, value); }
        internal DateTime DateTime { get => LockAndGet(ref _dateTime); set => SetAndNotify(ref _dateTime, value); }

        public DocumentAdapter(EntityBase entity) : base(entity)
        {
            if (entity is not Document)
                throw new ArgumentException();

            Document document = entity as Document;
            Number = document.Number;
            DateTime = document.DateTime;
        }
    }
}
