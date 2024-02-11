using FluentValidation;
using Microsoft.AspNetCore.Http;
using MixServer.Application.Users.Extensions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Commands.LoginUser;

public class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, LoginCommandResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly IValidator<LoginUserCommand> _validator;

    public LoginUserCommandHandler(
        IHttpContextAccessor httpContextAccessor,
        IUserAuthenticationService userAuthenticationService,
        IValidator<LoginUserCommand> validator)
    {
        _httpContextAccessor = httpContextAccessor;
        _userAuthenticationService = userAuthenticationService;
        _validator = validator;
    }
    
    public async Task<LoginCommandResponse> HandleAsync(LoginUserCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);
        
        request.Audience = _httpContextAccessor.GetRequestAuthority();

        var response = await _userAuthenticationService.LoginAsync(request);

        return new LoginCommandResponse(response);
    }
}