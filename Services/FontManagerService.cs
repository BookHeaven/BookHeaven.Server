using MediatR;

namespace BookHeaven.Server.Services;

public class FontManagerService(ISender sender)
{
    public async Task DeleteFont(string familyName)
    {
        
    }
}