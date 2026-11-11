namespace BookHeaven.Server.Features.Authentication.Abstractions;

public interface IAuthenticationService
{
    Task LoginAsync(string name);
}