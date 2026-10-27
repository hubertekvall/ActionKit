#nullable enable
using Godot;

namespace ActionKit.Utilities;

public static class Camera3DExtensionMethods
{
    public static Vector2 UnprojectPositionScaleAdjusted(this Camera3D camera, Vector3 position)
    {
        var camViewportSize = camera.GetViewport().GetVisibleRect().Size;
        var screenSize = camera.GetTree().Root.GetVisibleRect().Size;
        var scale = screenSize / camViewportSize;


        return camera.UnprojectPosition(position) * scale;
    }



    public static Vector2 GetMousePositionScaleAdjusted(this Camera3D camera)
    {
        // Mouse position must come from the node's OWN viewport (root/window),
        // not camera.GetViewport() — the camera's SubViewport doesn't know
        // it's being displayed upscaled by the container.
        var rootMousePosition = camera.GetTree().Root.GetMousePosition();
        return rootMousePosition / camera.GetShrinkScale();
    }
    
    public static Vector2 GetShrinkScale(this Camera3D camera)
    { 
        return camera.GetTree().Root.GetVisibleRect().Size / camera.GetViewport().GetVisibleRect().Size;
    }
}