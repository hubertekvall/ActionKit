#nullable enable
using System.Linq;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Turns an actor "off": disables every component that was authored in the actor's scene and
/// fades out any HoverComponent. Components added at runtime (ComponentBreeder,
/// OnDamageAddComponents, OnDeathAddComponents) are left alone, so this can ship together
/// with the replacement behaviour, e.g. as a death template.
/// </summary>
[GlobalClass]
public partial class DisableSceneComponents : ActorComponent
{
	/// <summary>Seconds to fade hover strength to zero. 0 drops it at once.</summary>
	[Export] public float HoverFadeTime { get; set; } = 2.0f;

	public override void OnActorReady()
	{
		foreach (var component in ActorOwner.GetChildren().OfType<ActorComponent>())
		{
			if (component.Origin == ComponentOrigin.Scene && component is not HoverComponent)
				component.Enabled = false;
		}

		foreach (var hover in ActorOwner.GetComponents<HoverComponent>())
		{
			if (HoverFadeTime <= 0f)
			{
				hover.Strength = 0f;
				continue;
			}

			GetTree().CreateTween().TweenProperty(hover, (string)HoverComponent.PropertyName.Strength, 0.0, HoverFadeTime);
		}
	}
}
