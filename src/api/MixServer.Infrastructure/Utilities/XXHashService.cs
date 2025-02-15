using System.Security.Cryptography;
using System.Text;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Utilities;

public class XxHashService : IHashService
{
    public string Hash(string input)
    {
        // Convert the input string to a byte array and compute the hash.
        var data = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Create a new StringBuilder to collect the bytes
        // and create a string.
        var sb = new StringBuilder();

        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        foreach (var b in data)
        {
            sb.Append(b.ToString("x2"));
        }

        // Return the hexadecimal string.
        return sb.ToString();
    }
}