using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IUnitOfWork unitOfWork,
    IUserAuthenticationService userAuthenticationService,
    IValidator<ResetPasswordCommand> validator)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task HandleAsync(ResetPasswordCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        var currentUserName =
            (await currentUserRepository.GetCurrentUserAsync()).UserName ?? throw new UnauthorizedRequestException();

        await userAuthenticationService.ResetPasswordAsync(currentUserName, request.CurrentPassword, request.NewPassword);

        await unitOfWork.SaveChangesAsync();
    }
}