using BookHeaven.Core;
using Microsoft.Extensions.Options;

namespace BookHeaven.Server.Middlewares;

public class CoverNotFoundMiddleware(RequestDelegate next)
{

    public async Task InvokeAsync(HttpContext context, IOptions<CoreOptions> options, IWebHostEnvironment env)
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
        
        // If the file does not exist, return wwwroot/img/no-cover.jpg
        var noCoverPath = Path.Combine(env.WebRootPath, "img", "no-cover.jpg");
        if (!File.Exists(noCoverPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        
        context.Response.ContentType = "image/jpeg";
        await context.Response.SendFileAsync(noCoverPath);
    }
}