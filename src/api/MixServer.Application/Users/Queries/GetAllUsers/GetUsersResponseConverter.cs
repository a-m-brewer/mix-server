using MixServer.Application.Users.Dtos;
using MixServer.Domain.Interfaces;
using MixServer.Infrastructure.EF.Entities;

namespace MixServer.Application.Users.Queries.GetAllUsers;

public class GetUsersResponseConverter(
    IConverter<DbUser, UserDto> userDtoConverter)
    : IConverter<IEnumerable<DbUser>, GetAllUsersResponse>
{
    public GetAllUsersResponse Convert(IEnumerable<DbUser> value)
    {
        return new GetAllUsersResponse
        {
            Users = value.Select(userDtoConverter.Convert).ToList()
        };
    }
}