namespace BookHeaven.Server.Features.Settings.DTOs;

public record ServerSettings
{
    public string Culture { get; set; } = "en-US";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string LongDateFormat { get; set; } = "dddd, dd MMMM, yyyy";
}