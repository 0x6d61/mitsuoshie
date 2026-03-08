using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mitsuoshie.Core.Models;

public class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly Dictionary<string, DeployedToken> _tokensByPath = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<DeployedToken> Tokens => _tokensByPath.Values.ToList();

    public SettingsStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public void AddToken(DeployedToken token)
    {
        _tokensByPath[token.FilePath] = token;
    }

    public string? GetOriginalHash(string filePath)
    {
        return _tokensByPath.TryGetValue(filePath, out var token) ? token.OriginalHash : null;
    }

    public HoneyTokenType? GetHoneyType(string filePath)
    {
        return _tokensByPath.TryGetValue(filePath, out var token) ? token.HoneyType : null;
    }

    public bool ContainsPath(string filePath)
    {
        return _tokensByPath.ContainsKey(filePath);
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var data = new SettingsData
        {
            DeployedTokens = _tokensByPath.Values.ToList()
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public static SettingsStore Load(string filePath)
    {
        var store = new SettingsStore(filePath);

        if (!File.Exists(filePath))
            return store;

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);

        if (data?.DeployedTokens is not null)
        {
            foreach (var token in data.DeployedTokens)
            {
                store.AddToken(token);
            }
        }

        return store;
    }

    private record SettingsData
    {
        public List<DeployedToken> DeployedTokens { get; init; } = [];
    }
}
