using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Users.Commands.AddUser;

public class AddUserCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IUserAuthenticationService userAuthenticationService,
    IValidator<AddUserCommand> validator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddUserCommand, AddUserCommandResponse>
{
    public async Task<AddUserCommandResponse> HandleAsync(AddUserCommand request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await currentUserRepository.GetCurrentUserAsync();

        if (!currentUser.InRole(Role.Administrator) &&
            !currentUser.InRole(Role.Owner))
        {
            throw new ForbiddenRequestException();
        }

        var temporaryPassword = await userAuthenticationService.RegisterAsync(request.Username, request.Roles);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddUserCommandResponse
        {
            TemporaryPassword = temporaryPassword
        };
    }
}