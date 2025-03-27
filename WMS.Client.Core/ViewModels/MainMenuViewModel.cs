﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Models;

namespace WMS.Client.Core.ViewModels
{
    internal class MainMenuViewModel : ViewModelBase
    {

        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();

        internal ObservableCollection<Order> Orders { get => _orders; set => _orders = value; }

        internal RelayCommand GetOrdersCommand { get; }

        public MainMenuViewModel()
        {
            GetOrdersCommand = new RelayCommand((p) => GetOrders());
        }

        private async void GetOrders()
        {

            HttpClient client = new HttpClient();
            List<Order> orders = await client.GetFromJsonAsync<List<Order>>("http://vm-igmo-dev:8220/api/orders");

            _orders.Clear();
            orders.ForEach(_orders.Add);

        }

    }
}
