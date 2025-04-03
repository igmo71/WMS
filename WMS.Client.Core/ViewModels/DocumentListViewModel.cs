using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WMS.Client.Core.Infrastructure;
using WMS.Client.Core.Models;
using WMS.Client.Core.Services;

namespace WMS.Client.Core.ViewModels
{
    internal class DocumentListViewModel : PageViewModelBase
    {
        //private ObservableCollection<Order> _orders = new ObservableCollection<Order>();

        //internal ObservableCollection<Order> Orders { get => _orders; set => _orders = value; }

        //public RelayCommand OpenCommand { get; }

        //public DocumentListViewModel()
        //{
        //    Name = "Documents";
        //    OpenCommand = new RelayCommand((p) =>
        //    {
        //        if (p is Order order)
        //            NavigationService.AddPage(NavigationService.GetUniqueKey<DocumentViewModel>(order.Id.ToString()), () => new DocumentViewModel(order));
        //    });
        //    GetOrders();
        //}

        //private async void GetOrders()
        //{
        //    List<Order> orders = new List<Order>() 
        //    { 
        //        new Order() { Id = Guid.NewGuid(), DateTime = DateTime.UtcNow, Name = "Order", Number = "123" },
        //        new Order() { Id = Guid.NewGuid(), DateTime = DateTime.UtcNow, Name = "Order", Number = "4335" },
        //    };

        //    _orders.Clear();
        //    orders.ForEach(_orders.Add);

        //}
    }
}
