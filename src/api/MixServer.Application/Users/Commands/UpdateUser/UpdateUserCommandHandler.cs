using FluentValidation;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Validators;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
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
        
        unitOfWork.InvokeCallbackOnSaved(c => c.UserUpdated(otherUser));
        
        await unitOfWork.SaveChangesAsync();
    }
}