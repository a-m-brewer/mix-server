using JetBrains.Annotations;

namespace MixServer.Infrastructure.Server.Settings;

public class HostSettings
{
    public string ValidUrls { get; set; } = string.Empty;

    [UsedImplicitly]
    public List<string> ValidSchemes { get; set; } = [];

    [UsedImplicitly]
    public List<string> ValidDomains { get; set; } = [];

    [UsedImplicitly]
    public List<string> ValidPorts { get; set; } = [];

    public string[] ValidUrlsSplit => GenerateValidUrlsSplit();

    private IEnumerable<Uri> ValidUris => GenerateValidUrlsSplit()
        .Select(s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
        .Where(w => w != null)
        .Cast<Uri>()
        .ToArray();

    public IEnumerable<string> ValidAuthorities =>
        ValidUris
            .Select(s => s.Authority)
            .ToArray();

    private string[] GenerateValidUrlsSplit()
    {
        var initialValidUrls = string.IsNullOrWhiteSpace(ValidUrls)
            ? []
            : ValidUrls.Split(";");
        var validUrls = new List<string>(initialValidUrls);

        if (ValidSchemes.Count == 0 ||
            ValidDomains.Count == 0 ||
            ValidPorts.Count == 0)
        {
            return validUrls.Distinct().ToArray();
        }

        foreach (var scheme in ValidSchemes)
        {
            foreach (var domain in ValidDomains)
            {
                foreach (var port in ValidPorts)
                {
                    var validUrl = $"{scheme}://{domain}";
                    if (!string.IsNullOrWhiteSpace(port))
                    {
                        validUrl += $":{port}";
                    }
                    
                    validUrls.Add(validUrl);
                }
            }
        }

        return validUrls
            .Distinct()
            .ToArray();
    }
}