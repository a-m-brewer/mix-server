using FluentValidation;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Validators;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Users.Commands.DeleteUser;

public class DeleteDeviceCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IIdentityUserAuthenticationService userAuthenticationService,
    IValidator<DeleteUserCommand> validator,
    IUserValidator userValidator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var currentUser = currentUserRepository.CurrentUser;

        var otherUser = await userAuthenticationService.GetUserByIdOrThrowAsync(request.UserId);
        
        userValidator.AssertCanModifyUser(currentUser, otherUser);
        
        await userAuthenticationService.DeleteUserAsync(otherUser);

        await unitOfWork.SaveChangesAsync();
    }
}