using WMS.Client.Core.Adapters.Documents;
using WMS.Shared.Models.Documents;

namespace WMS.Client.Core.ViewModels.Documents
{
    internal class OrderOutViewModel : ViewModelBase
    {
        private readonly OrderOutAdapter _adapter;

        internal override string Title => $"{nameof(OrderOut)} {_adapter.Number} {_adapter.DateTime}";
        internal OrderOutAdapter Adapter => _adapter;

        internal OrderOutViewModel(OrderOutAdapter adapter)
        {
            _adapter = adapter;
        }
    }
}
