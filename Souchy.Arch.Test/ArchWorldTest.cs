using Arch.Core;
using Arch.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Arch.Test;

public class ArchWorldTest
{
    private static readonly Position v1 = new(Vector2.One);
    private static readonly Position v2 = new(Vector2.One * 2);

    [Fact]
    public void NothingToDo()
    {
        // World 1
        var world1 = World.Create();
        var entt1 = world1.Create(v1);
        world1.Destroy(entt1);
        world1.Dispose();

        // World 2
        var world2 = World.Create();
        var entt2 = world2.Create(v2);

        Assert.Equal(entt1, entt2);
        Assert.True(entt1.IsAlive());
        Assert.Equal(entt1.Get<Position>().Value, entt2.Get<Position>().Value);
    }

    [Fact]
    public void Entity_ComesBackToLife_InNewWorld_NewEntity()
    {
        // World 1
        var world1 = World.Create();
        var entt1 = world1.Create(v1);  // { id: 1, world: 1, version: 1, pos: [1,1] }

        // Destroy entity
        world1.Destroy(entt1);
        Assert.False(entt1.IsAlive());

        // Different version in the same world
        var entt2 = world1.Create(v1);  // { id: 1, world: 1, version: 2, pos: [1,1] }
        Assert.False(entt1.IsAlive());
        Assert.NotEqual(entt1.Version, entt2.Version);
        world1.Destroy(entt2);

        // Destroy world
        world1.Dispose();
        World.Destroy(world1); // -> calls world.Dispose()

        // Entity comes back to life in new world + new entity
        var world2 = World.Create();
        var entt3 = world2.Create(v2); // { id: 1, world: 1, version: 1, pos: [2,2] }
        Assert.True(entt1.IsAlive());
        Assert.True(world2.IsAlive(entt1));
        Assert.Equal(entt1, entt3);
        Assert.Equal(entt1.Get<Position>().Value, entt3.Get<Position>().Value);

        // Expected: World { Id = 1, Capacity = 0, Size = 0 }
        // Actual: World { Id = 1, Capacity = 819, Size = 1 }
        Assert.NotEqual(world1, world2);
    }

    [Fact]
    public void EntityReused_InNewWorldNewEntity_IfWorldDisposed()
    {
        // Arrange

        // World 1
        var world1 = World.Create();
        var entt1 = world1.Create(v1);
        world1.Dispose();

        // Entity is removed temporarily
        Assert.Throws<NullReferenceException>(() => entt1.IsAlive());
        Assert.Throws<NullReferenceException>(() => entt1.Get<Position>());
        Assert.Null(World.Worlds[entt1.WorldId]);

        // World 2
        var world2 = World.Create();

        // Entity can query the world, but is not inside it
        Assert.False(entt1.IsAlive());
        Assert.Throws<NullReferenceException>(() => entt1.Get<Position>());
        Assert.NotNull(World.Worlds[entt1.WorldId]);

        var entt2 = world2.Create(v2);

        // Entity back to life because of the new entity in world2
        Assert.True(entt1.IsAlive());
        Assert.True(world2.IsAlive(entt1));

        // Both worlds have the same id
        Assert.Equal(world1.Id, world2.Id);
        // Both entities have the same id
        Assert.Equal(entt1.Id, entt2.Id);
        // Somehow the same fucking version
        Assert.Equal(entt1.Version, entt2.Version);

        // Same entity
        Assert.Equal(entt2, entt1);
        Assert.Equal(entt2.Get<Position>().Value, entt1.Get<Position>().Value);

        // Using Entity.WorldId => gets world2 value
        Assert.Equal(world2, World.Worlds[entt1.WorldId]);
        Assert.Equal(v2.Value, world2.Get<Position>(entt1).Value);
        Assert.Equal(v2.Value, entt1.Get<Position>().Value);

        // Using old world reference => gets world1 value
        Assert.Equal(v1.Value, world1.Get<Position>(entt1).Value);
    }


    /// <summary>
    /// Entt1 destroy
    /// Entt2 has new version
    /// Entt1 stays dead, but gets entt2 values
    /// </summary>
    [Fact]
    public void EntityShouldDieWhenDestroyed()
    {
        var v1 = new Position(Vector2.One);
        var v2 = new Position(Vector2.One * 2);

        // Arrange
        var world = World.Create();
        var entt1 = world.Create(v1);
        world.Destroy(entt1);

        // Entity dead
        Assert.False(entt1.IsAlive());
        Assert.Throws<NullReferenceException>(() => entt1.Get<Position>());
        Assert.Throws<NullReferenceException>(() => world.Get<Position>(entt1));
        Assert.Equal(world, World.Worlds[entt1.WorldId]);

        var entt2 = world.Create(v2);

        // Entity still dead because new version
        Assert.False(entt1.IsAlive());

        // Same id
        Assert.Equal(entt1.Id, entt2.Id);
        // Same world
        Assert.Equal(entt1.WorldId, entt2.WorldId);
        // Same value
        Assert.Equal(entt2.Get<Position>().Value, entt1.Get<Position>().Value);

        // New version -> because the same id was using in the same world
        Assert.NotEqual(entt1.Version, entt2.Version);
    }

    [Fact]
    public void Entity_ThrowsException_AfterWorldDisposed()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.One),
            new Velocity(Vector2.One)
        );
        world.Dispose();

        var a = World.Worlds[entt.WorldId];
        var pos = world.Get<Position>(entt);

        Assert.Throws<NullReferenceException>(() => entt.IsAlive());
        //Assert.True(entt.IsAlive(), "Entity should not be alive after the world is disposed.");
        Assert.Null(World.Worlds[entt.WorldId]);
        Assert.Throws<NullReferenceException>(() => entt.Get<Position>());
    }
}
