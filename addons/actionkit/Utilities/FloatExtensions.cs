#nullable enable
namespace ActionKit.Utilities;

public static class FloatExtensions
{
    public static float Arch(this float x) => x.Square().Lerp(x.Flip().Square(), x);

    public static float SmoothStep2(this float x) => x.Square().Lerp(x.SmoothStop2(), x);
    public static float SmoothStop2(this float x) => x.Flip().Square().Flip();
    

    public static float Flip(this float x) => 1.0f - x;
    public static float Scale(this float x, float scale) => x * scale;
    public static float Square(this float x) => x * x;
    public static float Lerp(this float a, float b, float t) => a + (b - a) * t;
    public static float Mix(this float a, float b, float blend, float t) => a + blend * (b - a);

}