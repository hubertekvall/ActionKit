#nullable enable
namespace ActionKit.AI;

/// <summary>A typed blackboard key, so reads and writes can't disagree on the value type.</summary>
public sealed class BlackboardKey<T>
{
	public string Name { get; }
	public BlackboardKey(string name) => Name = name;
	public override string ToString() => Name;
}
