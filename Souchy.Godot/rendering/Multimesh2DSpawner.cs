using Godot;

namespace Souchy.Godot.rendering;

public enum MultimeshSpawnerFlags
{
    None = 0,
    Color = 1,
    CustomData = 2,
    All = Color | CustomData,
}

public class Multimesh2DSpawner
{
    public MultiMeshInstance2D MultiMeshInstance;
    public MultiMesh Multimesh;
    public int VisibleCount { get; set; }

    public int CurrentInstance { get; set; } = 0;

    public Multimesh2DSpawner(Texture2D texture, Vector2 quadSize, MultimeshSpawnerFlags flags = MultimeshSpawnerFlags.None)
    {
        bool useColors = (flags & MultimeshSpawnerFlags.Color) > 0;
        bool useCustomData = (flags & MultimeshSpawnerFlags.CustomData) > 0;
        MultiMeshInstance = new MultiMeshInstance2D()
        {
            Name = texture.ResourcePath.Split("/").Last(),
            Texture = texture,
            Multimesh = new MultiMesh()
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
                UseColors = useColors,
                UseCustomData = useCustomData,
                Mesh = new QuadMesh()
                {
                    Size = quadSize,
                },
            },
        };
        Multimesh = MultiMeshInstance.Multimesh;
    }

    public virtual void AddInstances(int count)
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
        }
    }

    public virtual void RemoveInstances(int count)
    {
        int newVisibleCount = Math.Max(0, VisibleCount - count);
        VisibleCount = newVisibleCount;

        Multimesh.VisibleInstanceCount = VisibleCount;
        //Array.Resize(ref buffer, Multimesh.InstanceCount * stride);
    }

    public virtual void RemoveInstance()
    {
        RemoveInstances(1);
    }

    public virtual void UpdateInstanceTransform(int i, Vector2 position, Vector2 velocity)
    {
        var t = new Transform2D(velocity.Angle(), position);
        Multimesh.SetInstanceTransform2D(i, t);
    }

    public virtual void UpdateInstanceTransformColor(int i, Vector2 position, Vector2 velocity, Color color)
    {
        UpdateInstanceTransform(i, position, velocity);
        Multimesh.SetInstanceColor(i, color);
    }
    public virtual void UpdateInstanceTransformColorData(int i, Vector2 position, Vector2 velocity, Color color, Color customData)
    {
        UpdateInstanceTransformColor(i, position, velocity, color);
        Multimesh.SetInstanceCustomData(i, customData);
    }

    public virtual void UpdateInstance(Vector2 position, Vector2 velocity)
    {
        UpdateInstanceTransform(CurrentInstance, position, velocity);
        CurrentInstance++;
    }
    public virtual void UpdateInstance(Vector2 position, Vector2 velocity, Color color)
    {
        UpdateInstanceTransformColor(CurrentInstance, position, velocity, color);
        CurrentInstance++;
    }
    public virtual void UpdateInstance(Vector2 position, Vector2 velocity, Color color, Color customData)
    {
        UpdateInstanceTransformColorData(CurrentInstance, position, velocity, color, customData);
        CurrentInstance++;
    }

    public virtual void SendToGodot()
    {
        //Multimesh.VisibleInstanceCount = VisibleCount = CurrentInstance;
        CurrentInstance = 0;
    }

}
