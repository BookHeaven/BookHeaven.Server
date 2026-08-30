using BookHeaven.Server.Features.Authentication.Models;

namespace BookHeaven.Server.Features.Authentication.Abstractions;

public interface IAuthenticationService
{
    Task<SelectedProfile?> GetSelectedProfileAsync();
    Task LoginAsync(string name);
}