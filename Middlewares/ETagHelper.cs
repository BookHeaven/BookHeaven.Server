using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace BookHeaven.Server.Middlewares
{
    public static class ETagHelper
    {
        private static string ComputeETag(FileInfo fileInfo)
        {
            var lastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();
            var length = fileInfo.Length;
            return $"\"{lastModified:x}-{length:x}\"";
        }

        public static bool TryHandleIfNoneMatch(HttpContext context, FileInfo fileInfo)
        {
            var request = context.Request;
            var response = context.Response;
            var eTag = ComputeETag(fileInfo);

            response.Headers.ETag = eTag;

            if (!request.Headers.TryGetValue("If-None-Match", out var incoming)) return false;
            
            var matches = incoming.ToString().Split(',').Select(v => v.Trim());
            if (!matches.Contains(eTag)) return false;
            
            response.StatusCode = StatusCodes.Status304NotModified;
            response.ContentLength = 0;
            return true;

        }
    }
}
