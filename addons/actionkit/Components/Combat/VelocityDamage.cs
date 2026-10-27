#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Components;

// Ram damage. Only the part of the impact this actor is responsible for counts: the closing
// speed along the contact normal, capped by how fast we ourselves were moving into the other
// actor. So a parked ship deals nothing to its rammer, a fleeing ship soaks up less, and a
// head-on crash hurts both sides. The rammer always takes some damage back.
[GlobalClass]
public partial class VelocityDamage : ActorComponent
{
    /// <summary>Closing speed below this does no damage (bumps and scrapes).</summary>
    [Export] public float MinImpactSpeed = 8.0f;
    /// <summary>Closing speed at which damage reaches MaxDamage.</summary>
    [Export] public float MaxImpactSpeed = 40.0f;
    [Export] public int MaxDamage = 100;
    /// <summary>Our mass / their mass is clamped to [1/limit, limit] and scales the damage we deal.</summary>
    [Export] public float MassRatioLimit = 2.0f;
    /// <summary>Share of the hit we take ourselves, before mass scaling. Heavier rammers take less.</summary>
    [Export] public float SelfDamageFraction = 0.5f;
    /// <summary>Seconds before the same actor can be rammed again. Contacts repeat every physics tick.</summary>
    [Export] public float HitCooldown = 0.5f;

    private Vector3 _velocity;
    private Vector3 _previousVelocity;
    private readonly Dictionary<ulong, double> _lastHitTime = new();

    public override void PhysicsTick(double delta)
    {
        if (ActorOwner.ActorBody == null) return;
        _previousVelocity = _velocity;
        _velocity = ActorOwner.ActorBody.LinearVelocity;
    }

    public override void OnMessage(ActorMessage message)
    {
        if (message is not ActorCollisionMessage collision) return;
        var body = ActorOwner.ActorBody;
        var other = collision.OtherActor;
        if (body == null || !IsInstanceValid(other)) return;

        var now = Time.GetTicksMsec() / 1000.0;
        var id = other.GetInstanceId();
        if (_lastHitTime.TryGetValue(id, out var last) && now - last < HitCooldown) return;

        // Contact normals point different ways for different body types, so face it toward the other actor.
        var contact = collision.CollisionData;
        var normal = contact.Normal.Normalized();
        if (normal.Dot(contact.OtherPosition - body.GlobalPosition) < 0) normal = -normal;

        // A rigid body's velocity has already bounced by the time its contacts arrive,
        // so also look at last tick's velocity and take whichever hit harder.
        var ownSpeed = Mathf.Max(_velocity.Dot(normal), _previousVelocity.Dot(normal));
        var closingSpeed = ownSpeed - contact.OtherVelocity.Dot(normal);
        var speed = Mathf.Min(ownSpeed, closingSpeed);
        if (speed < MinImpactSpeed) return;

        if (_lastHitTime.Count > 32) _lastHitTime.Clear(); // forget old targets now and then
        _lastHitTime[id] = now;

        var t = Mathf.Clamp((speed - MinImpactSpeed) / (MaxImpactSpeed - MinImpactSpeed), 0f, 1f);
        var baseDamage = MaxDamage * t * t;

        var otherMass = other.ActorBody?.Mass ?? body.Mass;
        var massFactor = Mathf.Clamp(body.Mass / otherMass, 1f / MassRatioLimit, MassRatioLimit);

        var dealt = Mathf.Min(Mathf.RoundToInt(baseDamage * massFactor), MaxDamage);
        var taken = Mathf.RoundToInt(baseDamage * SelfDamageFraction / massFactor);

        if (dealt > 0) other.Broadcast(new DamageMessage(dealt, contact).Signed(this));
        if (taken > 0) ActorOwner.Broadcast(new DamageMessage(taken, contact).Signed(this));
    }
}
