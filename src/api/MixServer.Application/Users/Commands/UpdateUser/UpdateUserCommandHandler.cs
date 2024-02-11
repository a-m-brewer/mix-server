using FluentValidation;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Validators;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    ICallbackService callbackService,
    ICurrentUserRepository currentUserRepository,
    IIdentityUserAuthenticationService userAuthenticationService,
    IValidator<UpdateUserCommand> validator,
    IUserValidator userValidator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task HandleAsync(UpdateUserCommand request)
    {
        await validator.ValidateAndThrowAsync(request);
        
        var currentUser = currentUserRepository.CurrentUser;
        var otherUser = await userAuthenticationService.GetUserByIdOrThrowAsync(request.UserId);
        
        userValidator.AssertCanModifyUser(currentUser, otherUser);

        if (request.Roles is not null)
        {
            otherUser.Roles = request.Roles;
        }
        
        callbackService.InvokeCallbackOnSaved(c => c.UserUpdated(otherUser));
        
        await unitOfWork.SaveChangesAsync();
    }
}