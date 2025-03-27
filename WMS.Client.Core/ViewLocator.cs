using System;
using System.Reflection.Metadata.Ecma335;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using WMS.Client.Core.ViewModels;
using WMS.Client.Core.Views;

namespace WMS.Client.Core
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