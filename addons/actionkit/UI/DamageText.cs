#nullable enable
using ActionKit.Utilities;
using Godot;

namespace ActionKit.UI;

/// <summary>
/// A floating screen-space label pinned to a 3D position. Make a scene with a DamageText root
/// and a Label (inside a CanvasLayer or Control) assigned to Label, and add a
/// LifetimeComponent so it goes away. Spawned by DamageTextSpawner.
/// </summary>
[GlobalClass]
public partial class DamageText : Actor
{
	[Export] public Label? Label { get; set; }

	/// <summary>Camera to project with. Defaults to the viewport's active camera.</summary>
	[Export] public Camera3D? Camera { get; set; }

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (Engine.IsEditorHint() || Label == null) return;

		var camera = Camera ?? GetViewport().GetCamera3D();
		if (camera == null) return;

		Label.Visible = !camera.IsPositionBehind(GlobalPosition);
		Label.Position = camera.UnprojectPositionScaleAdjusted(GlobalPosition) - Label.Size / 2f;
	}
}
