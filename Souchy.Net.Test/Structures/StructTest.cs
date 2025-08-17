using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Net.Test.Structures;


// Record class cannot get updated at all
public class ParticleClass(Vector2 Position, Vector2 Velocity, Color Color)
{
    public Vector2 Position = Position;
    public Vector2 Velocity = Velocity;
    public Color Color = Color;
}
// Struct is ok if reassigned
public struct ParticleStruct(Vector2 Position, Vector2 Velocity, Color Color)
{
    public Vector2 Position = Position;
    public Vector2 Velocity = Velocity;
    public Color Color = Color;
}
// Record struct is ok if reassigned
public record struct ParticleRecordStruct(Vector2 Position, Vector2 Velocity, Color Color);

public class StructTest
{

    [Fact]
    public void StructGets_Updated()
    {
        ParticleStruct particle = new ParticleStruct(new(0, 0), new(1, 1), Color.White);
        particle.Position += particle.Velocity;
        Assert.Equal(new(1, 1), particle.Position);
    }

    [Fact]
    public void RecordStruct_GetsUpdated()
    {
        ParticleRecordStruct particle = new ParticleRecordStruct(new(0, 0), new(1, 1), Color.White);
        particle.Position += particle.Velocity;
        Assert.Equal(new(1, 1), particle.Position);
    }

    [Fact]
    public void Struct_GetsUpdated_InLoop_OnlyIfReassigned()
    {
        List<ParticleStruct> particles = new();
        for (int i = 0; i < 10; i++)
            particles.Add(new ParticleStruct(new(0, 0), new(1, 1), Color.White));

        // Error "Cannot modify members of p because it is a 'foreach iteration variable'"
        //foreach (var p in particles)
        //{
        //    p.Position += p.Velocity; // This won't update the list
        //}

        // Doesn't update because we copy the struct value into 'particle'
        for (int i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            particle.Position += particle.Velocity;
        }
        Assert.All(particles, p => Assert.Equal(new(0, 0), p.Position)); // Position should still be (0,0)

        // Reassign the list value to update
        for (int i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            particle.Position += particle.Velocity;
            particles[i] = particle; // Re-assign the updated struct back to the list
        }
        Assert.All(particles, p => Assert.Equal(new(1, 1), p.Position)); 
    }

    [Fact]
    public void RecordStruct_GetsUpdated_InLoop_OnlyIfReassigned()
    {
        List<ParticleRecordStruct> particles = new();
        for (int i = 0; i < 10; i++)
            particles.Add(new ParticleRecordStruct(new(0, 0), new(1, 1), Color.White));

        // Error "Cannot modify members of p because it is a 'foreach iteration variable'"
        //foreach (var p in particles)
        //{
        //    p.Position += p.Velocity; // This won't update the list
        //}

        // Doesn't update because we copy the struct value into 'particle'
        for (int i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            particle.Position += particle.Velocity;
        }
        Assert.All(particles, p => Assert.Equal(new(0, 0), p.Position)); // Position should still be (0,0)

        // Reassign the list value to update
        for (int i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            particle.Position += particle.Velocity;
            particles[i] = particle; // Re-assign the updated struct back to the list
        }
        Assert.All(particles, p => Assert.Equal(new(1, 1), p.Position));
    }


}
