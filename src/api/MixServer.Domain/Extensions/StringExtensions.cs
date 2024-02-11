using MixServer.Domain.Exceptions;

namespace MixServer.Domain.Extensions;

public static class StringExtensions
{
    private static string ValidChars => "abcdefghijklmnopqrstuvwxyz0123456789-_";
    
    public static string? ToValidHtmlId(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var prefix = char.IsLetter(value, 0)
            ? ""
            : "id_";

        var sanitized = new string(value
            .ToLowerInvariant()
            .Select(c => ValidChars.Contains(c) ? c : '_')
            .ToArray());

        return $"{prefix}{sanitized}";
    }
    
    public static string GetParentFolderPathOrThrow(this string absolutePath)
    {
        var folderAbsolutePath = GetParentFolderPathOrDefault(absolutePath);

        if (string.IsNullOrWhiteSpace(folderAbsolutePath))
        {
            throw new InvalidRequestException(nameof(absolutePath), "Directory Absolute Path is Null");
        }

        return folderAbsolutePath;
    }

    public static string? GetParentFolderPathOrDefault(this string absolutePath)
    {
        return Path.GetDirectoryName(absolutePath);
    }
}