﻿using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : PageViewModelBase
    {
        internal RelayCommand DocumentsCommand { get; }
        internal RelayCommand TestCommand { get; }

        public HomeViewModel()
        {
            Name = "Home";
            _persistent = true;

            DocumentsCommand = new RelayCommand((p) => NavigationService.AddPage(nameof(DocumentListViewModel), () => new DocumentListViewModel()));
            TestCommand = new RelayCommand((p) => NavigationService.AddPage(nameof(DocumentListViewModel), () => new DocumentListViewModel()));
        }
    }
}
