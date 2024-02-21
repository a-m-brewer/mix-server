using FluentValidation;
using Microsoft.AspNetCore.Http;
using MixServer.Application.Users.Extensions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Commands.RefreshUser;

public class RefreshUserCommandHandler(
    IUserAuthenticationService authenticationService,
    IHttpContextAccessor httpContextAccessor,
    IValidator<RefreshUserCommand> validator)
    : ICommandHandler<RefreshUserCommand, RefreshUserResponse>
{
    public async Task<RefreshUserResponse> HandleAsync(RefreshUserCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        request.Audience = httpContextAccessor.GetRequestAuthority();
        
        var response = await authenticationService.RefreshAsync(request);

        return new RefreshUserResponse(response);
    }
}