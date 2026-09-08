using Wms.Mobile.Services;

namespace Wms.Mobile;

public partial class ReceivingOrderReceivingPage
{
    private async void OnOrderRefreshing(object? sender, EventArgs e)
    {
        if (_busy || HasPendingCommand || (_mode != ReceivingPageMode.Ready && _mode != ReceivingPageMode.Scanning))
        {
            OrderRefreshView.IsRefreshing = false;
            return;
        }

        SetBusy(true);
        CameraScannerView.Stop();
        try
        {
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
