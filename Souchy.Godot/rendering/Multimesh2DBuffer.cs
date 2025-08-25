using Godot;

namespace Souchy.Godot.rendering;

public class Multimesh2DBuffer : Multimesh2DSpawner
{
    public float[] buffer;
    public int stride;
    public int WriteIndex = 0;

    public Multimesh2DBuffer(Texture2D texture, Vector2 quadSize, MultimeshSpawnerFlags flags = MultimeshSpawnerFlags.None) : base(texture, quadSize, flags)
    {
        bool useColors = (flags & MultimeshSpawnerFlags.Color) > 0;
        bool useCustomData = (flags & MultimeshSpawnerFlags.CustomData) > 0;
        stride = 8;
        stride += useColors ? 4 : 0;
        stride += useCustomData ? 4 : 0;
        buffer = [];
    }

    public override void AddInstances(int count)
    {
        // Convert existing instances to visible:
        int instanceCount = Multimesh.InstanceCount;
        int hiddenInstances = instanceCount - VisibleCount;
        int convertToVisible = Math.Min(hiddenInstances, count);
        VisibleCount += convertToVisible;

        // Add new instances:
        int remaining = count - convertToVisible;
        if (remaining > 0)
        {
            Multimesh.InstanceCount += remaining;
            Multimesh.VisibleInstanceCount += remaining;
            VisibleCount += remaining;
            Array.Resize(ref buffer, Multimesh.InstanceCount * stride);
        }
    }

    public override void UpdateInstanceTransform(int i, Vector2 position, Vector2 velocity)
    {
        var t = new Transform2D(velocity.Angle(), position);
        // calculate 2d transform and store in buffer
        buffer[WriteIndex + 0] = t.X.X;
        buffer[WriteIndex + 1] = t.Y.X;
        buffer[WriteIndex + 2] = 0;
        buffer[WriteIndex + 3] = t.Origin.X;
        buffer[WriteIndex + 4] = t.X.Y;
        buffer[WriteIndex + 5] = t.Y.Y;
        buffer[WriteIndex + 6] = 0;
        buffer[WriteIndex + 7] = t.Origin.Y;
        WriteIndex += 8;
    }

    public override void UpdateInstanceTransformColor(int i, Vector2 position, Vector2 velocity, Color color)
    {
        UpdateInstanceTransform(i, position, velocity);
        buffer[WriteIndex + 0] = color.R;
        buffer[WriteIndex + 1] = color.G;
        buffer[WriteIndex + 2] = color.B;
        buffer[WriteIndex + 3] = color.A;
        WriteIndex += 4;
    }

    public override void UpdateInstanceTransformColorData(int i, Vector2 position, Vector2 velocity, Color color, Color customData)
    {
        UpdateInstanceTransformColor(i, position, velocity, color);
        buffer[WriteIndex + 0] = color.R;
        buffer[WriteIndex + 1] = color.G;
        buffer[WriteIndex + 2] = color.B;
        buffer[WriteIndex + 3] = color.A;
        WriteIndex += 4;
    }

    public override void SendToGodot()
    {
        //Multimesh.SetInstanceCount(Multimesh.InstanceCount);
        //Multimesh.SetVisibleInstanceCount(VisibleCount);
        //Multimesh.VisibleInstanceCount = VisibleCount = CurrentInstance;

        Multimesh.Buffer = buffer;
        CurrentInstance = 0;
        WriteIndex = 0;
    }

}
