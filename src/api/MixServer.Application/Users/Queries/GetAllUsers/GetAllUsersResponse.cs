using MixServer.Application.Users.Dtos;

namespace MixServer.Application.Users.Queries.GetAllUsers;

public class GetAllUsersResponse
{
    public List<UserDto> Users { get; set; } = [];
}