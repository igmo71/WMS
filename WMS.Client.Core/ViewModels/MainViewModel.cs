using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WMS.Client.Core.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";
    }
}
