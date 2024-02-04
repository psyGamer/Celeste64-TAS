
using Celeste64.TAS.Render;

namespace Celeste64;

public class DeathBlock : Actor, IHaveRenderCollider
{
    public void RenderCollider(Batcher3D batch)
    {
        batch.Line(WorldBounds.Min, WorldBounds.Min with { X = WorldBounds.Max.X }, Color.Red);
        batch.Line(WorldBounds.Min, WorldBounds.Min with { Y = WorldBounds.Max.Y }, Color.Red);
        batch.Line(WorldBounds.Min, WorldBounds.Min with { Z = WorldBounds.Max.Z }, Color.Red);

        batch.Line(WorldBounds.Max, WorldBounds.Max with { X = WorldBounds.Min.X }, Color.Red);
        batch.Line(WorldBounds.Max, WorldBounds.Max with { Y = WorldBounds.Min.Y }, Color.Red);
        batch.Line(WorldBounds.Max, WorldBounds.Max with { Z = WorldBounds.Min.Z }, Color.Red);

        batch.Line(WorldBounds.Min with { Y = WorldBounds.Max.Y }, WorldBounds.Max with { X = WorldBounds.Min.X }, Color.Red);
        batch.Line(WorldBounds.Min with { Y = WorldBounds.Max.Y }, WorldBounds.Max with { Z = WorldBounds.Min.Z }, Color.Red);

        batch.Line(WorldBounds.Max with { Y = WorldBounds.Min.Y }, WorldBounds.Min with { X = WorldBounds.Max.X }, Color.Red);
        batch.Line(WorldBounds.Max with { Y = WorldBounds.Min.Y }, WorldBounds.Min with { Z = WorldBounds.Max.Z }, Color.Red);

        batch.Line(WorldBounds.Min with { X = WorldBounds.Max.X }, WorldBounds.Max with { Z = WorldBounds.Min.Z }, Color.Red);
        batch.Line(WorldBounds.Min with { Z = WorldBounds.Max.Z }, WorldBounds.Max with { X = WorldBounds.Min.X }, Color.Red);

        batch.Box(WorldBounds.Min, WorldBounds.Max, Color.Red * 0.5f);
    }
}
