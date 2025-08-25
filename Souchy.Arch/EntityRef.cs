using Arch.Core;

namespace Souchy.Arch;

public record struct EntityRef(Entity Entity, int WorldVersion) : IEquatable<EntityRef>
{
    public bool IsAlive()
    {
        var world = World.Worlds[Entity.WorldId];
        if (world == null) return false;
        if (world.GetVersion() != WorldVersion) return false;
        return world.IsAlive(Entity);
    }

    public bool TryGet<T>(out T component) where T : struct
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

    public T Get<T>()
    {
        var world = World.Worlds[Entity.WorldId];
        return world.Get<T>(Entity);
    }

}
