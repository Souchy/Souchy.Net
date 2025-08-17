using Arch.Core;

namespace Souchy.Arch;

public static class WorldVersion
{
    private static Dictionary<int, int> Versions = new();

    public static int GetVersion(this World world)
    {
        if (!Versions.TryGetValue(world.Id, out var version))
        {
            version = 0;
            Versions[world.Id] = version;
        }
        return version;
    }

    public static void RegisterVersion(this World world)
    {
        if (!Versions.TryGetValue(world.Id, out int value))
        {
            Versions[world.Id] = 0;
        }
        else
        {
            Versions[world.Id] = ++value;
        }
    }

    public static void Reset()
    {
        Versions.Clear();
    }

    [Obsolete("Should not use this method. Use RegisterVersion instead")]
    public static void SetVersion(this World world, int version)
    {
        Versions[world.Id] = version;
    }

    public static EntityRef GetRef(this Entity entity)
    {
        var world = World.Worlds[entity.WorldId];
        return new EntityRef(entity, world.GetVersion());
    }
}
