
using Celeste64.TAS.Render;

namespace Celeste64;

public class SpikeBlock : Attacher, IHaveModels, IHaveDebugRender
{
	public SimpleModel? Model;
	public Vec3 Direction;

	public override Vec3 AttachNormal => -Direction;
	public override Vec3 AttachOrigin => WorldBounds.Center - WorldBounds.Size * Direction * .5f;

	public override void Added()
	{
		// TODO: improve spike rotation resolution. this was kinda hacked in
		// and feels like it could be better (ex. pick "longest" axis, rotate around that till we find a wall)

		var size = LocalBounds.Size;
		var step = 6.4f;

		Vec3 forward;
		Vec3 horizontal;
		Vec3 vertical;
		Matrix rotation;

		if (size.Z < size.X && size.Z < size.Y)
		{
			forward = Vec3.UnitZ;
			horizontal = Vec3.UnitX;
			vertical = Vec3.UnitY;
			rotation = Matrix.Identity;
		}
		else if (size.X < size.Y)
		{
			forward = -Vec3.UnitX;
			horizontal = Vec3.UnitZ;
			vertical = Vec3.UnitY;
			rotation = Matrix.CreateRotationY(-MathF.PI / 2);
		}
		else
		{
			forward = -Vec3.UnitY;
			horizontal = Vec3.UnitX;
			vertical = Vec3.UnitZ;
			rotation = Matrix.CreateRotationX(MathF.PI / 2);
		}

		// flip due to solid test?
		if (World.SolidRayCast(Position, forward, 10, out _))
		{
			forward = -forward;

			if (forward.X != 0)
				rotation = Matrix.CreateRotationY(MathF.PI / 2);
			else
				rotation = Matrix.CreateRotationX(-MathF.PI / 2);
		}

		var width = (horizontal * size).Length();
		var height = (vertical * size).Length();
		var columns = Math.Max(1, MathF.Round(width / step));
		var rows = Math.Max(1, MathF.Round(height / step));
		var models = new List<SkinnedModel>();

		for (int x = 0; x < columns; x++)
			for (int y = 0; y < rows; y++)
			{
				models.Add(new SkinnedModel(Assets.Models["spike"])
				{
					Flags = ModelFlags.Terrain,
					Transform =
					Matrix.CreateScale(2.5f) *
					rotation *
					Matrix.CreateTranslation(
						horizontal * ((x + 0.5f) * step - width / 2) +
						vertical * ((y + 0.5f) * step - height / 2) +
						-forward * (step / 2))
				});
			}

		Model = new SimpleModel(models);
		Direction = forward;

		base.Added();
	}

	public void CollectModels(List<(Actor Actor, Model Model)> populate)
	{
		if (Model != null)
			populate.Add((this, Model));
	}

    public void RenderDebug(Batcher3D batch)
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
