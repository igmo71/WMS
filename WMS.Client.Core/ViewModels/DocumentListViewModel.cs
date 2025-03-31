using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Models;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : ViewModelBase
    {
        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();

        internal ObservableCollection<Order> Orders { get => _orders; set => _orders = value; }

        public DocumentListViewModel()
        {
            Name = "Documents";
            GetOrders();
        }

        private async void GetOrders()
        {
            List<Order> orders = new List<Order>() { new Order() { Id = Guid.NewGuid(), DateTime = DateTime.UtcNow, Name = "Some name", Number = "123" } };

            _orders.Clear();
            orders.ForEach(_orders.Add);

        }
    }
}
