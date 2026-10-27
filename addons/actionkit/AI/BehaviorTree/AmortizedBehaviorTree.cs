#nullable enable
using Godot;

namespace ActionKit.AI;

/// <summary>
/// A behavior tree that ticks only every TickInterval physics frames. Each instance gets a
/// different phase, so many AI actors spread their work across frames.
/// </summary>
[GlobalClass]
public partial class AmortizedBehaviorTree : BTree
{
	private static ulong _nextOffset;
	private ulong _offset;

	[Export] public ulong TickInterval { get; set; } = 10;

	public override void _Ready()
	{
		base._Ready();
		_offset = _nextOffset++;
	}

	public override bool CanTick() =>
		TickInterval <= 1 || (Engine.GetPhysicsFrames() + _offset) % TickInterval == 0;
}
