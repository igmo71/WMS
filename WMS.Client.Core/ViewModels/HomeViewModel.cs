﻿using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class HomeViewModel : ViewModelBase
    {
        internal RelayCommand DocumentsCommand { get; }

        public HomeViewModel()
        {
            Name = "Home";
            _persistent = true;

            DocumentsCommand = new RelayCommand((p) => NavigationService.AddPage(new DocumentListViewModel()));
        }

    }
}
