using MixServer.Application.Users.Dtos;
using MixServer.Domain.Users.Models;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Users.Converters;

public class UserDtoConverter 
    : IConverter<DbUser, UserDto>,
        IConverter<IUser, UserDto>
{
    public UserDto Convert(DbUser value) => Convert((IUser)value);

    public UserDto Convert(IUser value)
    {
        return new UserDto
        {
            UserId = value.Id,
            Username = value.UserName ?? string.Empty,
            Roles = value.Roles.ToList()
        };
    }
}