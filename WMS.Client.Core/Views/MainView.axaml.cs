using Avalonia.Controls;

namespace WMS.Client.Core.Views
{
    public partial class MainView : UserControl
    {
        public MainView() => InitializeComponent();

        private void SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            bool portrait = e.NewSize.Height > e.NewSize.Width;
            CurrentName.IsVisible = portrait;
            PagesButton.IsVisible = portrait;
            PagesTabs.IsVisible = !portrait;
        }
    }
}