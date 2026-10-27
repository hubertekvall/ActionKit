#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Base for SpawnComponent variants that spawn Scenes at a random point on the owning
/// actor on their own timer, instead of reacting to a message. The
/// spawn point is drawn from the combined AABB of the visual geometry under
/// Actor.ModelPivot when one is assigned (so effects land on the model itself — sparks
/// across a damaged hull, steam along a pipe run), or otherwise from a sphere of Radius
/// around the actor's origin.
///
/// Subclasses only need to say when spawning should be happening (IsSpawning) and what
/// starting it means (Activate) — interval rolling, the AABB lookup and the actual spawn
/// call all live here. See TimedRandomSpawnEffectsComponent (runs for a fixed Duration)
/// and ToggledRandomSpawnEffectsComponent (runs for as long as an Enabled flag is set).
/// </summary>
public abstract partial class RandomAreaSpawnEffectsComponent : SpawnComponent
{
	[ExportGroup("Spawn Area")]
	[Export] public float Radius { get; set; } = 1.0f;

	[ExportGroup("Timing")]
	// Randomized rather than fixed so a run of spawns doesn't read as a metronome — each
	// gap is re-rolled between these two right after every spawn.
	[Export] public float MinInterval { get; set; } = 0.5f;
	[Export] public float MaxInterval { get; set; } = 1.5f;

	private double _spawnTimer;
	private Aabb? _modelPivotAabb;

	public override void OnActorReady()
	{
	
		base.OnActorReady();
		_modelPivotAabb = ComputeModelPivotAabb();
	}

	public override void PhysicsTick(double delta)
	{
	
		if (!IsSpawning()) return;
	
		_spawnTimer -= delta;
		if (_spawnTimer > 0.0) return;
		
		Spawn(GetRandomSpawnTransform(), ActorOwner);
		RollNextInterval();
	}

	/// <summary>Whether the countdown toward the next spawn should be running right now.</summary>
	protected abstract bool IsSpawning();

	/// <summary>
	/// Starts (or restarts) spawning right now. What that means depends on the subclass —
	/// TimedRandomSpawnEffectsComponent resets its countdown, ToggledRandomSpawnEffectsComponent
	/// flips Enabled on — so anything holding a plain RandomAreaSpawnEffectsComponent
	/// reference (e.g. a health-threshold trigger) can kick off either variant without
	/// caring which one it's actually pointed at.
	/// </summary>
	public virtual void Activate() => RollNextInterval();

	private void RollNextInterval() =>
		_spawnTimer = GD.RandRange(MinInterval, MaxInterval);

	protected virtual Transform3D GetRandomSpawnTransform()
	{
		var pivot = ActorOwner.ModelPivot;

		if (pivot != null && _modelPivotAabb is { } aabb)
		{
			var localPoint = aabb.Position + new Vector3(
				(float)GD.RandRange(0.0, aabb.Size.X),
				(float)GD.RandRange(0.0, aabb.Size.Y),
				(float)GD.RandRange(0.0, aabb.Size.Z));

			return new Transform3D(pivot.GlobalBasis, pivot.ToGlobal(localPoint));
		}

		return new Transform3D(ActorOwner.GlobalBasis, ActorOwner.GlobalPosition + RandomPointInSphere(Radius));
	}

	// Uniform-in-volume sample: random direction on the unit sphere, radius cube-rooted so
	// points fill the volume evenly instead of clustering at the center. Same construction
	// RandomPointInRadius/SpawnerImpulse use for theirs.
	private static Vector3 RandomPointInSphere(float radius)
	{
		var direction = new Vector3(
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0));

		if (direction.LengthSquared() < 0.0001f)
			direction = Vector3.Up; // degenerate guard; any fixed axis works

		var distance = radius * Mathf.Pow((float)GD.Randf(), 1.0f / 3.0f);
		return direction.Normalized() * distance;
	}

	// Combined local-space AABB of every VisualInstance3D under ModelPivot (mesh, particles,
	// etc.), so a multi-mesh ship model gets one bounding box covering all of it rather than
	// just whichever piece happens to sit at the pivot itself. Null when there's no pivot or
	// nothing visual under it, which tells GetRandomSpawnTransform to fall back to Radius.
	private Aabb? ComputeModelPivotAabb()
	{
		var pivot = ActorOwner.ModelPivot;
		if (pivot == null) return null;

		var visuals = pivot.FindDescendantsOfType<VisualInstance3D>();
		if (visuals.Count == 0) return null;

		Aabb? combined = null;
		foreach (var visual in visuals)
		{
			// Visual instance's local AABB, brought into pivot-local space by its transform
			// relative to the pivot (not the world — we only want ModelPivot's own local
			// frame to store this in, since that's what GetRandomSpawnTransform reads it
			// back against).
			var relative = pivot.GlobalTransform.AffineInverse() * visual.GlobalTransform;
			var localAabb = TransformAabb(relative, visual.GetAabb());
			combined = combined is { } existing ? existing.Merge(localAabb) : localAabb;
		}

		return combined;
	}

	// Encloses aabb's 8 corners after applying transform — the AABB equivalent of
	// transforming a point, done by hand since it's just as cheap and doesn't depend on
	// which AABB operators this Godot version's C# bindings happen to expose.
	private static Aabb TransformAabb(Transform3D transform, Aabb aabb)
	{
		var min = Vector3.Inf;
		var max = -Vector3.Inf;

		for (var i = 0; i < 8; i++)
		{
			var corner = transform * aabb.GetEndpoint(i);
			min.X = Mathf.Min(min.X, corner.X);
			min.Y = Mathf.Min(min.Y, corner.Y);
			min.Z = Mathf.Min(min.Z, corner.Z);
			max.X = Mathf.Max(max.X, corner.X);
			max.Y = Mathf.Max(max.Y, corner.Y);
			max.Z = Mathf.Max(max.Z, corner.Z);
		}

		return new Aabb(min, max - min);
	}
}
