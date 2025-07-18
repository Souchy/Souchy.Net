using Newtonsoft.Json;

namespace Souchy.Net.io;

public class Config
{
    public static string BaseDirectory { get; set; } = "";

    [JsonIgnore]
    private string _savePath = "";

    public static T Load<T>(string name = "") where T : Config, new()
    {
        if (name == "")
            name = typeof(T).Name;
        if (!name.Contains('.'))
            name += ".json";

        string path = name;
        if (!string.IsNullOrEmpty(BaseDirectory))
            path = Path.Combine(BaseDirectory, name);

        if (!File.Exists(path))
        {
            var t = new T();
            t._savePath = path;
            t.Save();
            return t;
        }

        string json = File.ReadAllText(path);
        T config = Json.Deserialize<T>(json) ?? throw new Exception($"Failed to deserialize config from {path}");
        config._savePath = path;
        return config;
    }

    public void Save()
    {
        string json = Json.Serialize(this);
        File.WriteAllText(_savePath, json);
    }

}
