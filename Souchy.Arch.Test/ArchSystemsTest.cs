using Arch.Core;
using Arch.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Arch.Test;

public class ArchSystemsTest
{

    [Fact]
    public void Arch_Query_ShouldWork()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        // Act
        world.Query(in new QueryDescription().WithAll<Position, Velocity>(), (Entity entity, ref Position pos, ref Velocity vel) =>
        {
            pos.Value += vel.Value;
        });
        // Assert
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }

    [Fact]
    public void Arch_LowLevel_ShouldWork()
    {
        // Arrange
        var world = World.Create();
        var entt = world.Create(
            new Position(Vector2.Zero),
            new Velocity(Vector2.One)
        );
        // Act
        var query = world.Query(in new QueryDescription().WithAll<Position, Velocity>());
        foreach (ref var chunk in query.GetChunkIterator())
        {
            var references = chunk.GetFirst<Position, Velocity>();
            foreach (var i in chunk)
            {
                ref var position = ref Unsafe.Add(ref references.t0, i);
                ref var collisionLayer = ref Unsafe.Add(ref references.t1, i);
                position.Value += collisionLayer.Value;
            }
        }
        // Assert
        Assert.Equal(Vector2.One, entt.Get<Position>().Value);
    }

}
