namespace MixServer.Infrastructure.Files.Constants;

public static class MimeTypeConstants
{
    public const string AudioFlac = "audio/flac";
    
    public const string ApplicationOgg = "application/ogg";
    public const string VideoOgg = "video/ogg";
    public const string AudioOgg = "audio/ogg";
    
    // see https://www.rfc-editor.org/rfc/rfc2046.txt 4.5.1
    // The "octet-stream" subtype is used to indicate that a body contains arbitrary binary data.
    public const string DefaultMimeType = "application/octet-stream";
}