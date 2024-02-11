using System.Text.Json.Serialization;
using MixServer.Domain.Users.Requests;

namespace MixServer.Application.Users.Commands;

public class TokenCommand : ITokenRequest
{
    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public string Audience { get; set; } = string.Empty;
}