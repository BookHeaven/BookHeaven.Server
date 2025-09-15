using BookHeaven.Domain.Abstractions;
using BookHeaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BookHeaven.Server.Services;

public class AlertService(ISnackbar snackbar, IDialogService dialogService) : IAlertService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        await dialogService.ShowMessageBox("title", message, cancel);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var result = await dialogService.ShowMessageBox(title, (MarkupString)message, accept, cancel);
        return result == true;
    }

    public Task<string> ShowPromptAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        throw new NotImplementedException();
    }

    public Task ShowToastAsync(string message, AlertSeverity severity = AlertSeverity.Info)
    {
        var mudSeverity = severity switch
        {
            AlertSeverity.Info => Severity.Info,
            AlertSeverity.Error => Severity.Error,
            AlertSeverity.Warning => Severity.Warning,
            AlertSeverity.Success => Severity.Success,
            _ => Severity.Normal
        };
        
        snackbar.Add(message, mudSeverity);
        return Task.CompletedTask;
    }
}