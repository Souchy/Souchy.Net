using Arch.Core;

namespace Souchy.Arch;

public record struct EntityRef(Entity Entity, int WorldVersion) : IEquatable<EntityRef>
{
    public readonly bool IsAlive()
    {
        var world = World.Worlds[Entity.WorldId];
        if (world == null) return false;
        return world.IsAlive(Entity) && world.GetVersion() == WorldVersion;
    }

    public readonly bool TryGet<T>(out T component) where T : struct
    {
        var world = World.Worlds[Entity.WorldId];
        if (world == null)
        {
            component = default;
            return false;
        }
        if (WorldVersion != world.GetVersion())
        {
            component = default;
            return false;
        }
        if (!world.IsAlive(Entity))
        {
            component = default;
            return false;
        }
        return world.TryGet(Entity, out component);
    }

    public readonly T Get<T>() where T : struct
    {
        var world = World.Worlds[Entity.WorldId];
        if (world == null) throw new NullReferenceException("World is null");
        if (world.GetVersion() != WorldVersion) throw new InvalidOperationException("Entity is from a different world version");
        return world.Get<T>(Entity);
    }
}
