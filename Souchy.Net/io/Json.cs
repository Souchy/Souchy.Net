using Newtonsoft.Json;

namespace Souchy.Net.io;

public static class Json
{

    public static JsonSerializerSettings Settings { get; set; } = new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    public static string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, Settings);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

}
