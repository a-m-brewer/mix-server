using Microsoft.AspNetCore.Http;
using MixServer.Domain.Exceptions;

namespace MixServer.Application.Users.Extensions;

public static class HttpContextExtensions
{
    public static string GetRequestAuthority(this IHttpContextAccessor contextAccessor)
    {
        if (contextAccessor.HttpContext == null)
        {
            throw new UnauthorizedRequestException();
        }

        var request = contextAccessor.HttpContext.Request;

        var host = request.Host.Value;

        return host;
    }
}