#nullable enable
using System.Collections.Generic;
using Godot;

namespace ActionKit.Components;

/// <summary>
/// Breaks MeshInstance into debris actors when the owner dies. Pieces come from Fracture, a set
/// of patterns baked in the editor with the Bake Fracture button, so a death only costs spawning
/// the actors. Without a baked set it falls back to slicing at runtime (MeshFracturer).
///
/// Editor workflow: set SliceCount and PatternCount, press Bake Fracture, then set PreviewPattern
/// to look at a pattern exploded next to the ship. Reroll Preview replaces just that one.
///
/// Any ActorComponents placed as children of this node (LifetimeComponent, ...) are templates,
/// duplicated onto every debris actor, same as SpawnComponent.
/// </summary>
[Tool]
[GlobalClass]
public partial class MeshSlicer : ActorComponent
{
	[ExportToolButton("Bake Fracture")] public Callable BakeButton => Callable.From(BakeFracture);
	[ExportToolButton("Reroll Preview")] public Callable RerollButton => Callable.From(RerollPreview);

	[Export] public MeshInstance3D? MeshInstance { get; set; }
	[Export(PropertyHint.Range, "2,32")] public int SliceCount { get; set; } = 6;
	[Export(PropertyHint.Range, "1,8")] public int PatternCount { get; set; } = 3;
	[Export] public FractureSet? Fracture { get; set; }

	// Overrides every surface of every piece. Leave empty to use MeshInstance's current materials.
	// Both are applied at spawn, so changing either needs no rebake.
	[Export] public Material? DebrisMaterial { get; set; }
	[Export] public bool SliceOnDeath { get; set; } = true;

	[Export]
	public int PreviewPattern
	{
		get => _previewPattern;
		set
		{
			_previewPattern = value;
			UpdatePreview();
		}
	}

	private int _previewPattern = -1;
	private Node3D? _preview;

	public override void _Ready()
	{
		if (Engine.IsEditorHint()) UpdatePreview();
	}

	public override void OnActorReady()
	{
		if (SliceOnDeath) ActorOwner.IsKilled += OnKilled;
	}

	public override void OnActorDetached()
	{
		if (SliceOnDeath) ActorOwner.IsKilled -= OnKilled;
	}

	private void OnKilled() => Slice();

	public List<Actor> Slice()
	{
		var debris = new List<Actor>();
		if (!IsInstanceValid(MeshInstance) || MeshInstance!.Mesh == null) return debris;

		var pattern = Fracture is { Patterns.Count: > 0 }
			? Fracture.Patterns[GD.RandRange(0, Fracture.Patterns.Count - 1)]
			: MeshFracturer.Fracture(MeshInstance, SliceCount);

		var tree = GetTree();
		var parent = tree.CurrentScene ?? tree.Root;
		var meshTransform = MeshInstance.GlobalTransform;

		for (var i = 0; i < pattern.Meshes.Count; i++)
		{
			var center = pattern.Centers[i];
			var actor = CreateDebris(pattern.Meshes[i], pattern.Shapes[i], center);

			foreach (var child in GetChildren())
			{
				if (child is ActorComponent template)
					actor.AddChild(template.Duplicate());
			}

			var world = meshTransform.TranslatedLocal(center);
			actor.Transform = parent is Node3D parent3D ? parent3D.GlobalTransform.AffineInverse() * world : world;
			parent.AddChild(actor, forceReadableName: true);
			debris.Add(actor);
		}

		MeshInstance.Visible = false;
		return debris;
	}

	/// <summary>
	/// Builds the actor for one piece. The piece mesh is centered on its own origin; center is
	/// where that origin sat in the source mesh, measured from the mesh's origin.
	/// </summary>
	protected virtual Actor CreateDebris(ArrayMesh mesh, Shape3D shape, Vector3 center)
	{
		var actor = new Actor { Name = "Debris" };
		actor.AddChild(CreatePieceMesh(mesh));
		return actor;
	}

	protected MeshInstance3D CreatePieceMesh(ArrayMesh mesh)
	{
		var piece = new MeshInstance3D { Mesh = mesh };
		if (DebrisMaterial != null)
		{
			piece.MaterialOverride = DebrisMaterial;
			return piece;
		}

		if (!IsInstanceValid(MeshInstance)) return piece;
		for (var i = 0; i < mesh.GetSurfaceCount(); i++)
		{
			if (int.TryParse(mesh.SurfaceGetName(i), out var source))
				piece.SetSurfaceOverrideMaterial(i, MeshInstance!.GetActiveMaterial(source));
		}
		return piece;
	}


// ---------------------------------------------------------------- Editor tooling

	private void BakeFracture()
	{
		if (!Engine.IsEditorHint() || MeshInstance?.Mesh == null) return;

		// Refill the existing set in place so it keeps its file path.
		var set = Fracture ?? new FractureSet();
		set.Patterns.Clear();
		for (var i = 0; i < PatternCount; i++)
			set.Patterns.Add(MeshFracturer.Fracture(MeshInstance, SliceCount));

		SaveFracture(set);
	}

	private void RerollPreview()
	{
		if (!Engine.IsEditorHint() || MeshInstance?.Mesh == null) return;
		if (Fracture == null || PreviewPattern < 0 || PreviewPattern >= Fracture.Patterns.Count) return;

		Fracture.Patterns[PreviewPattern] = MeshFracturer.Fracture(MeshInstance, SliceCount);
		SaveFracture(Fracture);
	}

	private void SaveFracture(FractureSet set)
	{
		var path = set.ResourcePath;
		if (string.IsNullOrEmpty(path))
		{
			var sceneDir = GetTree().EditedSceneRoot?.SceneFilePath.GetBaseDir();
			path = $"{(string.IsNullOrEmpty(sceneDir) ? "res:/" : sceneDir)}/{Owner?.Name ?? Name}_fracture.res";
		}

		ResourceSaver.Save(set, path, ResourceSaver.SaverFlags.ChangePath);
		Fracture = set;
		GD.Print($"{Name}: baked {set.Patterns.Count} fracture patterns to {path}. Save the scene to keep the reference.");
		UpdatePreview();
	}

	// Shows the previewed pattern exploded beside the source mesh. The preview nodes have no owner, so
	// they are never saved into the scene.
	private void UpdatePreview()
	{
		if (!Engine.IsEditorHint() || !IsInsideTree()) return;

		_preview?.QueueFree();
		_preview = null;

		if (MeshInstance?.Mesh == null || Fracture == null) return;
		if (PreviewPattern < 0 || PreviewPattern >= Fracture.Patterns.Count) return;

		var pattern = Fracture.Patterns[PreviewPattern];
		var bounds = MeshInstance.Mesh.GetAabb();
		_preview = new Node3D { TopLevel = true };
		AddChild(_preview);
		_preview.GlobalTransform = MeshInstance.GlobalTransform.TranslatedLocal(Vector3.Right * bounds.Size.X * 1.5f);

		for (var i = 0; i < pattern.Meshes.Count; i++)
		{
			var explode = (pattern.Centers[i] - bounds.GetCenter()) * 0.4f;
			var piece = CreatePieceMesh(pattern.Meshes[i]);
			piece.Position = pattern.Centers[i] + explode;
			_preview.AddChild(piece);
		}
	}
}
