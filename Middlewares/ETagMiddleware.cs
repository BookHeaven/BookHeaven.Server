using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BookHeaven.Server.Middlewares;

public class ETagMiddleware(RequestDelegate next, string requestPrefix, string physicalBasePath)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly string _requestPrefix = requestPrefix ?? throw new ArgumentNullException(nameof(requestPrefix));
    private readonly string _physicalBasePath = physicalBasePath ?? throw new ArgumentNullException(nameof(physicalBasePath));

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if (!request.Path.StartsWithSegments(_requestPrefix, out var remaining))
        {
            await _next(context);
            return;
        }

        var relPath = (remaining.HasValue ? remaining.Value : string.Empty).TrimStart('/');
        if (string.IsNullOrEmpty(relPath))
        {
            await _next(context);
            return;
        }

        var physicalPath = Path.Combine(_physicalBasePath, relPath.Replace('/', Path.DirectorySeparatorChar));
        var fileInfo = new FileInfo(physicalPath);
        if (!fileInfo.Exists)
        {
            await _next(context);
            return;
        }

        // Use shared helper for ETag logic
        if (ETagHelper.TryHandleIfNoneMatch(context, fileInfo))
        {
            return;
        }

        await _next(context);
    }
}