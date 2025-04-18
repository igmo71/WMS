using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using WMS.Client.Core.ViewModels;
using WMS.Client.Core.Views;

namespace WMS.Client.Core.Services
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            Func<Control> func = param switch
            {
                HomeViewModel => () => new HomeView(),
                OrderInViewModel => () => new OrderInView(),
                OrderOutViewModel => () => new OrderOutView(),
                CatalogListViewModel => () => new CatalogListView(),
                ProductViewModel => () => new ProductView(),
                DocumentListViewModel => () => new DocumentListView(),
                _ => () => new TextBlock { Text = "Not Found: " + param.GetType().FullName }
            };

            return func.Invoke();
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}