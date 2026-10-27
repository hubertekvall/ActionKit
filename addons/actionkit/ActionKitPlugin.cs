#if TOOLS
using Godot;

namespace ActionKit;

/// <summary>
/// Editor entry point. ActionKit doesn't need the plugin to be enabled to work: every node
/// and resource is a [GlobalClass], and the runtime managers (AudioEmitterManager,
/// ParticleEmitterManager) create themselves on first use. Enabling it only lists ActionKit
/// under Project Settings > Plugins.
/// </summary>
[Tool]
public partial class ActionKitPlugin : EditorPlugin
{
}
#endif
