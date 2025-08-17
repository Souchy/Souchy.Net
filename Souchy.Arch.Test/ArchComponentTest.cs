using Arch.Core;
using Arch.Core.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Souchy.Arch.Test;

public class ArchComponentTest
{

    [Fact]
    public void TestRef()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        QueryDescription movementQuery = new QueryDescription().WithAll<Position, Velocity>();

        // Act
        world.Query(in movementQuery, (Entity entity, ref Position pos, ref Velocity vel) =>
        {
            pos.Value += vel.Value;
        });
        world.Query(in movementQuery, (entity) =>
        {
            entity.Get<Position>().Value += entity.Get<Velocity>().Value;
        });

        // Entity just fetches the component from the world, so yes it's up to date. TODO: Test entity when world is deleted.
        Assert.Equal(Vector2.One * 2, entt.Get<Position>().Value);
    }

    [Fact]
    public void UpdatingEntityComponentValueInlineAffectsEntity()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );

        // Act
        entt.Get<Position>().Value += entt.Get<Velocity>().Value;

        // Assert that the position has been updated in the entity
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }

    [Fact]
    public void UpdatingEntityComponentValueInTwoLinesCopiesTheStruct()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );

        // Act
        var pos = entt.Get<Position>();
        pos.Value += entt.Get<Velocity>().Value;

        // Assert that the struct is a copy
        Assert.Equal(Vector2.One, pos.Value);
        Assert.Equal(Vector2.Zero, entt.Get<Position>().Value);
    }

    [Fact]
    public void UpdatingEntityComponentInTwoLinesRequiresRef()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );

        // Act
        ref var pos = ref entt.Get<Position>();
        pos.Value += entt.Get<Velocity>().Value;

        // Assert that the struct is a copy
        Assert.Equal(Vector2.One, pos.Value);
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }

    [Fact]
    public void UpdatingRecordStructValueWorks()
    {
        // Arrange
        var p = new Position(Vector2.Zero);
        var v = new Velocity(Vector2.One);

        // Act
        p.Value += v.Value;

        // Assert that the position has been updated
        Assert.Equal(Vector2.One, p.Value);
    }

    [Fact]
    public void UpdatingRefUpdatesAllStructs()
    {
        // Arrange
        var p = new Position(Vector2.Zero);
        ref var posRef = ref p;

        // Act
        posRef = new(Vector2.One);

        // Assert that the position has been updated
        Assert.Equal(Vector2.One, posRef.Value);
        Assert.Equal(Vector2.One, p.Value);
    }

}
