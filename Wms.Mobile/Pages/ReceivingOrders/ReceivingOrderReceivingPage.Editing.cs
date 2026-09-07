using System.Collections.ObjectModel;
using System.Globalization;
using Wms.Contracts.Mobile.V1;
using Wms.Mobile.Scanning;
using Wms.Mobile.Services;

namespace Wms.Mobile;

public partial class ReceivingOrderReceivingPage
{
    private void OnOpenLineSearchTapped(object? sender, TappedEventArgs e)
    {
        if (!CanStartNewAction)
        {
            return;
        }

        SetMode(ReceivingPageMode.Searching);
        CameraScannerView.Stop();
        Dispatcher.Dispatch(() => LineSearchEntry.Focus());
    }

    private async void OnCancelLineSearchTapped(object? sender, TappedEventArgs e)
    {
        await ClearSearchAsync();
        ReturnToScanning();
        await UpdateCameraAsync();
    }

    private async void OnLineSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        var version = ++_searchVersion;
        SetSearchBusy(false);
        SearchCandidates = [];
        OnPropertyChanged(nameof(SearchCandidates));
        if (_mode != ReceivingPageMode.Searching)
        {
            return;
        }

        var query = e.NewTextValue?.Trim() ?? string.Empty;
        if (query.Length < 2)
        {
            LineSearchStatusLabel.Text = "Введите не менее двух символов.";
            return;
        }

        try
        {
            await Task.Delay(300);
            if (!IsCurrentSearch(version))
            {
                return;
            }

            SetSearchBusy(true);
            var result = await _orderClient.SearchLinesAsync(
                Details.Order.Id,
                query);
            if (!IsCurrentSearch(version))
            {
                return;
            }

            SearchCandidates = result.Items;
            OnPropertyChanged(nameof(SearchCandidates));
            LineSearchStatusLabel.Text = result.HasMore
                ? "Показаны первые результаты. Уточните запрос."
                : $"Найдено: {result.Items.Count}.";
        }
        catch (MobileApiException exception)
        {
            if (IsCurrentSearch(version))
            {
                LineSearchStatusLabel.Text = exception.Message;
            }
        }
        catch (HttpRequestException)
        {
            if (IsCurrentSearch(version))
            {
                LineSearchStatusLabel.Text = "Сервер WMS недоступен.";
            }
        }
        finally
        {
            if (IsCurrentSearch(version))
            {
                SetSearchBusy(false);
            }
        }
    }

    private bool IsCurrentSearch(int version) =>
        version == _searchVersion && _mode == ReceivingPageMode.Searching;

    private async void OnSearchCandidateTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MobileReceivingOrderLineCandidateResponse candidate)
        {
            return;
        }

        var line = LineStates.Single(x => x.LineNumber == candidate.LineNumber);
        await ClearSearchAsync();
        BeginQuantityEdit(line);
    }

    private void OnEditQuantityTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ReceivingOrderLineViewState line)
        {
            BeginQuantityEdit(line);
        }
    }

    private void BeginQuantityEdit(ReceivingOrderLineViewState line)
    {
        if (!CanStartNewAction && _mode != ReceivingPageMode.Searching)
        {
            return;
        }

        CameraScannerView.Stop();
        _editingLine = line;
        SetMode(ReceivingPageMode.Editing);
        AccentLine(line.LineNumber, null);
        line.BeginEditing();
        InstructionLabel.Text = "Введите итоговое фактическое количество, включая 0.";
    }

    private async void OnSaveQuantityClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ReceivingOrderLineViewState line })
        {
            await SaveQuantityAsync(line);
        }
    }

    private async Task SaveQuantityAsync(ReceivingOrderLineViewState line)
    {
        if (_busy || !ReferenceEquals(_editingLine, line))
        {
            return;
        }

        if (_process.PendingQuantityLineNumber is int pendingLine && pendingLine != line.LineNumber)
        {
            ErrorLabel.Text = "Сначала повторите сохранение предыдущей строки.";
            return;
        }

        if (!TryReadQuantity(line, out var quantity))
        {
            return;
        }

        SetBusy(true);
        ErrorLabel.Text = string.Empty;
        try
        {
            var result = await _process.SetQuantityAsync(
                Details.Order.Id,
                line.LineNumber,
                quantity);
            if (result.ErrorMessage is not null)
            {
                ErrorLabel.Text = result.ErrorMessage;
                return;
            }

            var response = result.Response
                ?? throw new InvalidOperationException("Процесс приёмки не вернул результат команды.");
            line.EndEditing();
            _editingLine = null;
            ApplyDetails(response.Details);
            AccentLine(line.LineNumber, "Итог");
            ReturnToScanning();
        }
        catch (MobileApiException exception)
        {
            ErrorLabel.Text = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorLabel.Text = "Ответ сервера не получен. Повторите сохранение этого количества.";
        }
        finally
        {
            SetBusy(false);
            await UpdateCameraAsync();
        }
    }

    private async void OnCancelQuantityClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ReceivingOrderLineViewState line }
            || _busy
            || !ReferenceEquals(_editingLine, line))
        {
            return;
        }

        if (_process.IsQuantityPending)
        {
            ErrorLabel.Text = "Сначала повторите сохранение количества.";
            return;
        }

        line.EndEditing();
        _editingLine = null;
        ReturnToScanning();
        await UpdateCameraAsync();
    }

}

