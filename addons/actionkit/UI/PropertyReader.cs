#nullable enable
using Godot;

namespace ActionKit.UI;

/// <summary>
/// Inspector-only data binding. Put it under a UI element: every frame it reads Property off
/// Source and, only when the value changed, writes it to Target on its parent.
/// Property is a Godot property path: "Weapon:Ammo" reads Source.Weapon.Ammo. Source defaults
/// to the scene root (Owner). Target uses Godot's names, so built-in controls are snake_case
/// ("text", "value"), while your own C# properties keep their C# name ("Value").
/// </summary>
[GlobalClass]
public partial class PropertyReader : Node
{
	[Export] public Node? Source { get; set; }
	[Export] public string Property { get; set; } = "";
	[Export] public StringName Target { get; set; } = "text";

	/// <summary>Optional, e.g. "{0:0000}". When set, numbers are written as formatted text.</summary>
	[Export] public string Format { get; set; } = "";

	/// <summary>Emitted with the new value every time it changes, before Target is written. Leave Target empty to only use the signal.</summary>
	[Signal] public delegate void ChangedEventHandler(Variant value);

	private Node? _source;
	private Node? _target;
	private NodePath _path = "";
	private double _lastNumber = double.NaN;
	private string? _lastText;

	public override void _Ready()
	{
		_source = Source ?? Owner;
		_target = GetParent();
		// Exported as a string so it can be typed in; NodePath exports only show a node picker.
		_path = new NodePath(Property);
	}

	public override void _Process(double delta)
	{
		if (_source == null || _target == null || Property == "") return;

		var value = _source.GetIndexed(_path);
		// Nothing there yet (e.g. a reference that isn't set).
		if (value.VariantType == Variant.Type.Nil) return;

		if (value.VariantType is Variant.Type.String or Variant.Type.StringName)
		{
			var text = value.AsString();
			if (text == _lastText) return;
			_lastText = text;
			EmitSignalChanged(value);
			if (!Target.IsEmpty) _target.Set(Target, text);
			return;
		}

		// Numbers, bools and enums.
		if (value.VariantType is not (Variant.Type.Int or Variant.Type.Float or Variant.Type.Bool))
		{
			EmitSignalChanged(value);
			if (!Target.IsEmpty) _target.Set(Target, value);
			return;
		}

		var number = value.AsDouble();
		if (number == _lastNumber) return;
		_lastNumber = number;
		EmitSignalChanged(value);

		if (Target.IsEmpty) return;
		if (Format == "") _target.Set(Target, value);
		else _target.Set(Target, string.Format(Format, number));
	}
}
