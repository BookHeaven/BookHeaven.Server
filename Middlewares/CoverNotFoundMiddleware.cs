using BookHeaven.Core;
using Microsoft.Extensions.Options;

namespace BookHeaven.Server.Middlewares;

public class CoverNotFoundMiddleware(RequestDelegate next)
{

    public async Task InvokeAsync(HttpContext context, IOptions<CoreOptions> options)
    {
        var request = context.Request;
        if (!request.Path.StartsWithSegments("/covers", out var remaining))
        {
            await next(context);
            return;
        }
        
        
        var relPath = (remaining.HasValue ? remaining.Value : string.Empty).TrimStart('/');
        if (string.IsNullOrEmpty(relPath))
        {
            await next(context);
            return;
        }

        var physicalPath = Path.Combine(options.Value.CoversPath, relPath.Replace('/', Path.DirectorySeparatorChar));
        var fileInfo = new FileInfo(physicalPath);
        if (fileInfo.Exists)
        {
            await next(context);
            return;
        }
        
        // If the cover does not exist, return /img/no-cover.jpg
        context.Response.Headers["X-Fallback-Reason"] = "Cover not found";
        context.Response.Headers["X-Requested-Path"] = request.Path.ToString();
        context.Response.Redirect("/img/no-cover.jpg", permanent: false);
    }
}