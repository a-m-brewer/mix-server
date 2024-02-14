using FluentValidation;
using Microsoft.AspNetCore.Http;
using MixServer.Application.Users.Extensions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Commands.LoginUser;

public class LoginUserCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IUserAuthenticationService userAuthenticationService,
    IValidator<LoginUserCommand> validator)
    : ICommandHandler<LoginUserCommand, LoginCommandResponse>
{
    public async Task<LoginCommandResponse> HandleAsync(LoginUserCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        request.Audience = httpContextAccessor.GetRequestAuthority();

        var response = await userAuthenticationService.LoginAsync(request);

        return new LoginCommandResponse(response);
    }
}