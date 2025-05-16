using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Users.Commands.AddUser;

public class AddUserCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IUserAuthenticationService userAuthenticationService,
    IValidator<AddUserCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddUserCommand, AddUserCommandResponse>
{
    public async Task<AddUserCommandResponse> HandleAsync(AddUserCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var currentUser = currentUserRepository.CurrentUser;

        if (!currentUser.InRole(Role.Administrator) &&
            !currentUser.InRole(Role.Owner))
        {
            throw new ForbiddenRequestException();
        }

        var temporaryPassword = await userAuthenticationService.RegisterAsync(request.Username, request.Roles);

        await unitOfWork.SaveChangesAsync();

        return new AddUserCommandResponse
        {
            TemporaryPassword = temporaryPassword
        };
    }
}