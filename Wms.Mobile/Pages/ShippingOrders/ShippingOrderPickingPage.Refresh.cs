using Wms.Mobile.Services;

namespace Wms.Mobile;

public partial class ShippingOrderPickingPage
{
    private async void OnOrderRefreshing(object? sender, EventArgs e)
    {
        if (_busy || HasPendingCommand || (_mode != PickingPageMode.Ready && _mode != PickingPageMode.Scanning && _mode != PickingPageMode.Completion))
        {
            OrderRefreshView.IsRefreshing = false;
            return;
        }

        SetBusy(true);
        CameraScannerView.Stop();
        try
        {
            _deviationConfirmed = false;
            await RefreshOrderAsync(clearError: true);
        }
        finally
        {
            OrderRefreshView.IsRefreshing = false;
            SetBusy(false);
            await UpdateCameraAsync();
        }
    }

    private async Task RefreshOrderAsync(bool clearError = false)
    {
        // Do not allow another transition on the previous successful assessment.
        _synchronization = null;
        try
        {
            ApplyDetails(await _orderClient.GetAsync(Details.Order.Id));
            if (clearError || !IsSynchronizationResolved)
                ErrorLabel.Text = string.Empty;
        }
        catch (MobileApiException exception)
        {
            ErrorLabel.Text = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorLabel.Text = "Не удалось обновить ордер. Проверьте соединение и потяните экран вниз повторно.";
        }
    }
}
