using FluentValidation;
using Microsoft.AspNetCore.Http;
using MixServer.Application.Users.Extensions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Services;

namespace MixServer.Application.Users.Commands.RefreshUser;

public class RefreshUserCommandHandler : ICommandHandler<RefreshUserCommand, RefreshUserResponse>
{
    private readonly IUserAuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<RefreshUserCommand> _validator;

    public RefreshUserCommandHandler(
        IUserAuthenticationService authenticationService,
        IHttpContextAccessor httpContextAccessor,
        IValidator<RefreshUserCommand> validator)
    {
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
        _validator = validator;
    }
    
    public async Task<RefreshUserResponse> HandleAsync(RefreshUserCommand request)
    {
        await _validator.ValidateAndThrowAsync(request);

        request.Audience = _httpContextAccessor.GetRequestAuthority();
        
        var response = await _authenticationService.RefreshAsync(request);

        return new RefreshUserResponse(response);
    }
}