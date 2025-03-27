namespace WMS.Client.Core.ViewModels
{
    internal partial class MainViewModel : ViewModelBase
    {
        public ViewModelBase CurrentPage { get; set; }

        public MainViewModel()
        {
            CurrentPage = new HomeViewModel();
        }
    }
}
