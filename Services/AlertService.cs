using BookHeaven.Domain.Abstractions;
using BookHeaven.Domain.Enums;
using MudBlazor;

namespace BookHeaven.Server.Services;

public class AlertService(ISnackbar snackbar) : IAlertService
{
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        throw new NotImplementedException();
    }

    public Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        throw new NotImplementedException();
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
            _ => Severity.Normal
        };
        
        snackbar.Add(message, mudSeverity);
        return Task.CompletedTask;
    }
}