using Wms.Mobile.Services;

namespace Wms.Mobile;

public partial class ShippingOrderShippingPage
{
    private async void OnOrderRefreshing(object? sender, EventArgs e)
    {
        if (_busy || _pendingShippingRequestId is not null)
        {
            OrderRefreshView.IsRefreshing = false;
            return;
        }

        SetBusy(true);
        
        try
        {
            await RefreshOrderAsync(clearError: true);
        }
        finally
        {
            OrderRefreshView.IsRefreshing = false;
            SetBusy(false);
            
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
