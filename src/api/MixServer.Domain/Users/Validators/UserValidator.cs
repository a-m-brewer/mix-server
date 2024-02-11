using MixServer.Domain.Exceptions;
using MixServer.Domain.Users.Enums;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Users.Validators;

public interface IUserValidator
{
    void AssertCanModifyUser(IUser currentUser, IUser otherUser);
}

public class UserValidator : IUserValidator
{
    public void AssertCanModifyUser(IUser currentUser, IUser otherUser)
    {
        if (!currentUser.IsAdminOrOwner())
        {
            throw new ForbiddenRequestException("You do not have permission to delete users");
        }
        
        if (currentUser.Id == otherUser.Id)
        {
            throw new ForbiddenRequestException("UserId", "You cannot modify yourself");
        }

        if (otherUser.InRole(Role.Owner))
        {
            throw new ForbiddenRequestException("UserId", "You cannot modify the owner");
        }

        if (!currentUser.InRole(Role.Owner) && otherUser.InRole(Role.Administrator))
        {
            throw new ForbiddenRequestException("UserId", "Admins can not modify other admins");
        }
    }
}