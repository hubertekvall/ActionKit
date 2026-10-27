#nullable enable
using Godot.Collections;
using Godot;

namespace ActionKit.Effects;

// The available ways to combine TextureA (the base) with TextureB (the blend layer).
// RGB is combined per-mode; alpha is always a straight lerp between the two source alphas.
public enum TextureBlendMode
{
    Mix,        // straight lerp between A and B (classic "opacity" blend)
    Add,        // A + B
    Subtract,   // A - B
    Multiply,   // A * B (darkens, good for masking/tinting)
    Screen,     // inverse-multiply (brightens, good for combining glows)
    Overlay,    // contrast-preserving combine of Multiply/Screen
    Darken,     // per-channel min(A, B)
    Lighten,    // per-channel max(A, B)
    Difference  // per-channel abs(A - B)
}

/// <summary>
/// A [Tool] Resource that IS a Texture2D — it blends TextureA and TextureB together and can
/// be dropped directly into any texture slot (Sprite2D.Texture, TextureRect.Texture,
/// TextureButton, GPUParticles2D texture, StyleBoxTexture, custom CanvasItem draws, ...),
/// the same way you'd use a plain ImageTexture or GradientTexture2D.
///
/// Assign TextureA/TextureB in the Inspector, pick a Mode and Factor, and it rebakes
/// automatically. Because it extends Texture2D and calls EmitChanged() after every bake,
/// anything displaying it (Sprite2D, TextureRect, ...) redraws itself automatically —
/// no manual refresh needed.
///
/// How it works under the hood: this class has no native RenderingServer texture of its
/// own. Instead it bakes the blend into a private ImageTexture and forwards the engine's
/// texture-drawing virtuals (_Draw/_DrawRect/_DrawRectRegion) to that texture's RID — this
/// is Godot's own documented pattern for a scripted Texture2D. That makes it fully usable
/// anywhere a CanvasItem draws a 2D texture.
///
/// 3D materials and shader `sampler2D` uniforms are a hard exception: Godot's 3D renderer
/// binds a texture by reading its RenderingServer RID directly, bypassing the draw-call
/// virtuals entirely, and there is no scriptable virtual that lets a script supply that RID
/// — this is a Godot engine limitation, not something any script (this one included) can
/// work around. For those slots, use the BakedTexture field below (drag it into the
/// material's texture slot in the Inspector) or GetInternalTexture() from code — either one
/// gives you the real underlying ImageTexture, which does have a genuine RID.
///
/// Notes:
/// - Baking runs a per-pixel CPU loop, so it's meant for editor-time/occasional runtime
///   use (e.g. procedural asset generation), not per-frame blending.
/// </summary>
[Tool]
[GlobalClass]
public partial class BlendedTexture : Texture2D
{
    private Texture2D? _textureA;
    private Texture2D? _textureB;
    private TextureBlendMode _mode = TextureBlendMode.Mix;
    private float _factor = 1.0f;
    private Vector2I _outputSize = Vector2I.Zero;

    private Image? _bakedImage;
    private ImageTexture? _internalTexture;

    [Export]
    public Texture2D? TextureA
    {
        get => _textureA;
        set { _textureA = value; Rebake(); }
    }

    [Export]
    public Texture2D? TextureB
    {
        get => _textureB;
        set { _textureB = value; Rebake(); }
    }

    [Export]
    public TextureBlendMode Mode
    {
        get => _mode;
        set { _mode = value; Rebake(); }
    }

    // How strongly the blend mode's result is mixed back over TextureA. 0 = TextureA
    // unchanged, 1 = the full blend-mode result.
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Factor
    {
        get => _factor;
        set { _factor = Mathf.Clamp(value, 0f, 1f); Rebake(); }
    }

    // Leave at (0, 0) to auto-use TextureA's size; set explicitly to force an output size
    // (both source textures are resized to match).
    [Export]
    public Vector2I OutputSize
    {
        get => _outputSize;
        set { _outputSize = value; Rebake(); }
    }

    // The real, baked ImageTexture backing this resource — read-only in the Inspector.
    // Drag THIS field (not the BlendedTexture resource itself) into a 3D material's
    // texture slot or a shader sampler2D uniform; see the class remarks above for why.
    [Export]
    public ImageTexture? BakedTexture
    {
        get => _internalTexture;
        private set { } // exported for drag-and-drop visibility only; always Rebake()-driven
    }

    // Same thing, for use from code (e.g. `material.AlbedoTexture = myBlend.GetInternalTexture();`).
    public ImageTexture? GetInternalTexture() => _internalTexture;

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() == PropertyName.BakedTexture)
        {
            var usage = property["usage"].As<PropertyUsageFlags>();
            property["usage"] = (int)(usage | PropertyUsageFlags.ReadOnly);
        }
    }

    public override int _GetWidth() => _internalTexture?.GetWidth() ?? _textureA?.GetWidth() ?? 1;
    public override int _GetHeight() => _internalTexture?.GetHeight() ?? _textureA?.GetHeight() ?? 1;

    public override bool _IsPixelOpaque(int x, int y)
    {
        if (_bakedImage == null)
            return true;
        if (x < 0 || y < 0 || x >= _bakedImage.GetWidth() || y >= _bakedImage.GetHeight())
            return false;
        return _bakedImage.GetPixel(x, y).A > 0.01f;
    }

    public override void _Draw(Rid toCanvasItem, Vector2 pos, Color modulate, bool transpose)
    {
        if (_internalTexture == null)
            return;
        RenderingServer.CanvasItemAddTextureRect(
            toCanvasItem, new Rect2(pos, GetSize()), _internalTexture.GetRid(), false, modulate, transpose);
    }

    public override void _DrawRect(Rid toCanvasItem, Rect2 rect, bool tile, Color modulate, bool transpose)
    {
        if (_internalTexture == null)
            return;
        RenderingServer.CanvasItemAddTextureRect(
            toCanvasItem, rect, _internalTexture.GetRid(), tile, modulate, transpose);
    }

    public override void _DrawRectRegion(
        Rid toCanvasItem, Rect2 rect, Rect2 srcRect, Color modulate, bool transpose, bool clipUv)
    {
        if (_internalTexture == null)
            return;
        RenderingServer.CanvasItemAddTextureRectRegion(
            toCanvasItem, rect, _internalTexture.GetRid(), srcRect, modulate, transpose, clipUv);
    }

    // Re-runs the blend and updates the internal texture. Called automatically whenever an
    // exported property changes, but you can also call it manually after mutating
    // TextureA/TextureB's contents in place.
    public void Rebake()
    {
        if (_textureA == null || _textureB == null)
            return;

        Image imageA = _textureA.GetImage();
        Image imageB = _textureB.GetImage();
        if (imageA == null || imageB == null)
            return;

        imageA = (Image)imageA.Duplicate();
        imageB = (Image)imageB.Duplicate();
        imageA.Convert(Image.Format.Rgba8);
        imageB.Convert(Image.Format.Rgba8);

        Vector2I size = _outputSize != Vector2I.Zero ? _outputSize : imageA.GetSize();
        if (imageA.GetSize() != size)
            imageA.Resize(size.X, size.Y, Image.Interpolation.Lanczos);
        if (imageB.GetSize() != size)
            imageB.Resize(size.X, size.Y, Image.Interpolation.Lanczos);

        Image result = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);

        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Color a = imageA.GetPixel(x, y);
                Color b = imageB.GetPixel(x, y);
                Color raw = ApplyMode(a, b);
                Color blended = a.Lerp(raw, _factor);
                float alpha = Mathf.Lerp(a.A, b.A, _factor);

                result.SetPixel(x, y, new Color(
                    Mathf.Clamp(blended.R, 0f, 1f),
                    Mathf.Clamp(blended.G, 0f, 1f),
                    Mathf.Clamp(blended.B, 0f, 1f),
                    Mathf.Clamp(alpha, 0f, 1f)));
            }
        }

        _bakedImage = result;
        if (_internalTexture == null)
            _internalTexture = ImageTexture.CreateFromImage(_bakedImage);
        else
            _internalTexture.SetImage(_bakedImage);

        // Notify anything holding this texture (Sprite2D, TextureRect, ...) to redraw, and
        // refresh the Inspector so the BakedTexture preview updates too.
        EmitChanged();
        NotifyPropertyListChanged();
    }

    private Color ApplyMode(Color a, Color b)
    {
        return _mode switch
        {
            TextureBlendMode.Mix => b,
            TextureBlendMode.Add => a + b,
            TextureBlendMode.Subtract => a - b,
            TextureBlendMode.Multiply => a * b,
            TextureBlendMode.Screen => Colors.White - (Colors.White - a) * (Colors.White - b),
            TextureBlendMode.Overlay => new Color(
                OverlayChannel(a.R, b.R),
                OverlayChannel(a.G, b.G),
                OverlayChannel(a.B, b.B),
                1f),
            TextureBlendMode.Darken => new Color(
                Mathf.Min(a.R, b.R), Mathf.Min(a.G, b.G), Mathf.Min(a.B, b.B), 1f),
            TextureBlendMode.Lighten => new Color(
                Mathf.Max(a.R, b.R), Mathf.Max(a.G, b.G), Mathf.Max(a.B, b.B), 1f),
            TextureBlendMode.Difference => new Color(
                Mathf.Abs(a.R - b.R), Mathf.Abs(a.G - b.G), Mathf.Abs(a.B - b.B), 1f),
            _ => b
        };
    }

    private static float OverlayChannel(float a, float b)
        => a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b);
}
