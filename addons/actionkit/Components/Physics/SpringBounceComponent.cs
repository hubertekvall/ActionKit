#nullable enable
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Spring-damps the owning Actor's ActorBody toward RestHeight, but with "bounce lock": a
/// normal spring overshoots on both sides of its target, dipping below rest before settling.
/// Here any dip below RestHeight is clamped back to the floor and the velocity that caused it
/// is reflected upward instead — so the body only ever bounces *up* from rest, never sinks
/// below it.
/// </summary>
[GlobalClass]
public partial class SpringBounceComponent : ActorComponent
{
    [ExportGroup("Spring Bounce")]
    [Export] public float RestHeight;
    [Export] public float Stiffness = 120f;
    [Export] public float Damping = 12f;

    // How much vertical velocity survives a bounce (1 = perfectly elastic, 0 = dead stop at rest).
    [Export(PropertyHint.Range, "0,1,0.01")] public float BounceRestitution = 0.6f;

    private IActorBody3D? _body;

    public override void OnActorReady()
    {
       _body = ActorOwner.ActorBody;
    }

    public override void PhysicsTick(double delta)
    {
       if (_body == null) return;

       float dt = (float)delta;
       float height = _body.GlobalPosition.Y;
       float velocityY = _body.LinearVelocity.Y;

       // Classic spring-damper, expressed as a force so it scales correctly with mass and
       // composes with whatever else (gravity, thrust, ...) is acting on the body this frame.
       float error = RestHeight - height;
       float accel = Stiffness * error - Damping * velocityY;
       _body.ApplyCentralForce(Vector3.Up * accel * _body.Mass);

       // Bounce lock: don't let the body rest below RestHeight. If it sank under the floor
       // this frame, snap it back up to the floor and reflect the downward velocity that
       // carried it there into an upward bounce instead.
       if (height < RestHeight)
       {
          var pos = _body.GlobalPosition;
          pos.Y = RestHeight;
          _body.GlobalPosition = pos;

          if (velocityY < 0f)
          {
             var vel = _body.LinearVelocity;
             vel.Y = -velocityY * BounceRestitution;
             _body.LinearVelocity = vel;
          }
       }
    }

    /// <summary>
    /// Nudge the spring, e.g. on landing, taking a hit, or firing a weapon. Positive kicks it
    /// up; negative just gets absorbed by the bounce lock on the next tick instead of a dip.
    /// </summary>
    public void Kick(float velocityImpulse)
    {
       _body?.ApplyCentralImpulse(Vector3.Up * velocityImpulse * _body.Mass);
    }
}
