using PasswordGenerator;

namespace MixServer.Infrastructure.Users.Services;

public interface IPasswordGeneratorService
{
    string Generate();
}

public class PasswordGeneratorService : IPasswordGeneratorService
{
    public string Generate()
    {
        return new Password(12).Next();
    }
}