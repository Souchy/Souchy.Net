using Arch.Core;
using Arch.Core.Extensions;
using System.Numerics;

namespace Souchy.Arch.Test;

public class WorldVersionTest
{

    private static readonly Position v1 = new(Vector2.One);
    private static readonly Position v2 = new(Vector2.One * 2);

    [Fact]
    public void VersionedWorlds()
    {
        // World 1
        var world1 = World.Create();
        world1.RegisterVersion(); // Version 0
        var entt1 = world1.Create(v1);
        var ref1 = entt1.GetRef();

        world1.Destroy(entt1);
        world1.Dispose();

        var world2 = World.Create();
        world2.RegisterVersion(); // Version 1
        var entt2 = world2.Create(v2);
        var ref2 = entt2.GetRef();

        // Not the same
        Assert.False(ref1.IsAlive());
        Assert.True(ref2.IsAlive());

        // Not the same
        Assert.NotEqual(ref1, ref2);
        Assert.Equal(entt1, entt2);

        // Current world version
        Assert.Equal(1, world1.GetVersion());
        Assert.Equal(1, world2.GetVersion());

        // Entity's world version
        Assert.Equal(0, ref1.WorldVersion);
        Assert.Equal(1, ref2.WorldVersion);

        // Regular Get<T> gives the new entity's value (bad)
        Assert.Equal(entt1.Get<Position>().Value, entt2.Get<Position>().Value);

        // Ref Get<T> gives gives an error (good)
        Assert.Throws<InvalidOperationException>(() => ref1.Get<Position>().Value);
    }

}
